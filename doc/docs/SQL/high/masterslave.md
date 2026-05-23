---
outline: deep
---

# 主从与多库

::: tip 功能概述
mooSQL 的主从与多库能力，解决三类问题：

1. **连哪个库**：读写分离、指定连接位、灾备切换、同步双写  
2. **库是否可用**：连接健康探测、失败标记、自动恢复  
3. **写后同步**：与已有 `useSlave` 异步复制插件并存，互不干扰  

路由在 **SQL 执行边界**（`DBExecutor.ExecuteCmd`）解析，不改变 `getInstance` 返回的锚点实例；临时路由通过 `SQLBuilder` 链式 API 或 `Executor.RouteContext` 配置。
:::


---

## 1. 核心概念

### 1.1 三层分工

| 层级 | 职责 | 典型用法 |
|------|------|----------|
| **MooClient** | 主从组注册、全局策略、Failover / 健康事件 | `configureGroup`、`MasterSlaveOptions` |
| **DBInsCash** | 实例池，按连接位缓存 `DBInstance` | `getInstance(0)` |
| **DBExecutor** | 执行作用域：事务 + 临时路由 | `RouteContext`、`ExecuteCmd` 内解析目标库 |

**锚点原则**：`getInstance(0)` 或 `db.useSQL()` 得到的 `DBLive` 是逻辑组的**锚点**（通常为主库连接位）。真正执行 SELECT / UPDATE 时，框架在 `ExecuteCmd` 内按 SQL 类型和路由上下文决定连哪台库，执行结束后恢复锚点，不污染 Builder 状态。

### 1.2 与异步复制插件的关系

| 机制 | 作用 | 配置入口 |
|------|------|----------|
| **读写路由 / Failover / 双写**（本文） | 决定本次 SQL 连哪台库、主挂如何切换 | `configureGroup`、`useReadReplica` 等 |
| **异步复制**（已有） | 主库 DML/DDL 成功后，异步投递到从库 | `BaseClientBuilder.useSlave()` |

两者正交：可先走主库写路由，再由 `ModifyMediator` 异步复制到 `AsyncReplica=true` 的从库；也可对单笔 SQL 使用 `.useDualWrite()` 同步 fan-out。

---

## 2. 快速开始

### 2.1 注册主从组

```csharp
var client = builder.Client; // 或 MooClient 实例
var cash = DBCash;           // DBInsCash

// 连接位 0 为主库，1/2 为从库
client.configureGroup(0, g => g
    .master(0)
    .readPolicy(ReadRoutePolicy.WeightedRandom)
    .readFallbackToMaster(true)
    .failover(FailoverMode.OnNextConnect)
    .addSlave(1, s => { s.ReadReplica = true; s.Weight = 2; })
    .addSlave(2, s => { s.ReadReplica = true; s.HotStandby = true; s.WriteEnabled = true; }));
```

也可通过 `DBInsCash.configureGroup(...)` 调用，内部委托给 `MooClient`。

### 2.2 读写分离查询

```csharp
var db = cash.getInstance(0);   // 锚点 = 主库连接位

// 注册主从组后，SELECT 会按 ReadPolicy 自动路由到可用从库
var list = db.useSQL()
    .select("Id", "Name")
    .from("User")
    .where("Status", 1)
    .query<User>();

// 显式声明「本次读走从库」（推荐用于报表等场景，语义更清晰）
var report = db.useSQL()
    .useReadReplica()
    .select("*").from("Order")
    .query();
```

### 2.3 写操作与灾切

```csharp
// 默认写 ActiveMaster（配置主或 Failover 后的新主）
db.useSQL()
    .set("Status", 2).from("Order").where("Id", id)
    .doUpdate();

// 单笔写操作启用激进灾切：连接失败时立即选举热备并重试一次（无活动事务时）
db.useSQL()
    .useFailover(FailoverMode.ImmediateOnFailure)
    .set("Status", 2).from("Order").where("Id", id)
    .doUpdate();
```

---

## 3. 主从组配置

### 3.1 代码配置（GroupBuilder）

| 方法 | 说明 |
|------|------|
| `master(position)` | 指定主库连接位 |
| `addSlave(position, configure)` | 添加从库并设置能力 |
| `readPolicy(policy)` | 读路由策略 |
| `readFallbackToMaster(bool)` | 从库全不可用时是否回退主库 |
| `failover(mode)` | 灾切模式 |
| `enableDualWrite(params int[] positions)` | 批量标记双写从库 |

全局默认策略：

```csharp
client.MasterSlaveOptions = new MasterSlaveOptions
{
    DefaultFailover = FailoverMode.OnNextConnect,
    DefaultReadPolicy = ReadRoutePolicy.WeightedRandom,
    ReadFallbackToMaster = true,
    DualWriteError = DualWriteErrorPolicy.MasterWins,
    OnFailover = ctx => Console.WriteLine($"Failover: {ctx.OldMaster?.config.index} -> {ctx.NewMaster?.config.index}")
};
```

按组覆盖（`Groups` 字典，key 为主连接位）：

```csharp
client.MasterSlaveOptions.Groups[0] = new GroupOverride
{
    Failover = FailoverMode.ImmediateOnFailure,
    ReadPolicy = ReadRoutePolicy.FirstAvailable,
    RequireReadReplica = true
};
```

### 3.2 XML 配置

在 `DBInsCash` 的 `configPath` 指向的配置文件中，可为每个 `database` 节点声明 `master` 与 `slave` 子节点。加载配置时会自动调用 `MasterSlaveConfigLoader`。

```xml
<database index="0" name="main">
  <master failover="OnNextConnect">
    <!-- 只读从库，权重 2 -->
    <slave index="1" readReplica="true" weight="2"/>
    <!-- 可读 + 可升主的热备 -->
    <slave index="2" readReplica="true" hotStandby="true" writeEnabled="true"/>
    <!-- 同步双写目标 -->
    <slave index="3" dualWrite="true" writeEnabled="true"/>
    <!-- 仅异步复制，不参与读路由 -->
    <slave index="4" asyncReplica="true"/>
  </master>
</database>
```

**向后兼容**：若从库未声明任何能力属性，默认 `asyncReplica="true"`，与旧版 `DataBase.slaves` 行为一致。

支持的 slave 属性：

| 属性 | 说明 |
|------|------|
| `readReplica` | 参与读路由 |
| `hotStandby` | 灾备热库，主挂时可被选举为新主 |
| `dualWrite` | 同步双写目标 |
| `asyncReplica` | 仅异步复制，不参与读/主选举 |
| `writeEnabled` | 是否允许写（DualWrite / HotStandby 需要） |
| `weight` | 读负载权重（用于 WeightedRandom） |

`master` 节点支持 `failover` 属性，取值见 [Failover 模式](#51-failover-模式)。

---

## 4. 从库能力模型

同一从库可同时具备多种能力（bool 组合，非互斥枚举）：

| 属性 | 含义 | 典型场景 |
|------|------|----------|
| `ReadReplica` | 参与读路由 | 读写分离 |
| `HotStandby` | 灾备热库，可升主 | 主库故障切换 |
| `DualWrite` | 同步双写目标 | 脑裂多写、强一致 fan-out |
| `AsyncReplica` | 仅异步复制 | 配合 `useSlave`，延迟可接受 |

辅助判断（运行时）：

- `CanRead`：`ReadEnabled && (ReadReplica || HotStandby)`
- `CanFailover`：`HotStandby && WriteEnabled && 健康可用`
- `CanDualWrite`：`DualWrite && WriteEnabled`

**示例**：`ReadReplica=true && HotStandby=true` 表示「平时可读、主挂时可升主」。

---

## 5. 读写路由

### 5.1 路由判定优先级

`ExecuteCmd` 内按以下顺序解析目标实例（高 → 低）：

1. **活动事务**：已开启事务 → 锁定 Session 所在实例，忽略读从库与 Failover  
2. **RouteContext 显式覆盖**：`TargetInstance` / `TargetPosition` / `ForceMaster` / `PreferReadReplica`  
3. **SQL 类型**：`Select` → 读路径；`Insert`/`Update`/`Delete`/`Merge`/DDL → 写路径；`Unknown` 保守走写  
4. **组级默认**：`MasterSlaveOptions` + `GroupOverride` + 组的 `ReadPolicy` / `FailoverMode`

解析结果**仅用于当次 ExecuteCmd**，执行后恢复 Builder 锚点。

### 5.2 读路由策略（ReadRoutePolicy）

| 策略 | 行为 |
|------|------|
| `MasterOnly` | 全部走主库，不分离 |
| `RoundRobin` | 可用从库轮询 |
| `WeightedRandom` | 按 `Weight` 加权随机（组默认） |
| `FirstAvailable` | 第一个可用从库 |
| `Custom` | 使用 `CustomReadSelector` 或 `RouteContext.ReadSelector` |

从库候选条件：`CanRead` 且健康状态为 `Available` 或 `None`（尚未探测）。

**从库全不可用**时：

- `ReadFallbackToMaster = true`（默认）→ 回退 `ActiveMaster`  
- 否则抛出 `NoReadableReplicaException`

### 5.3 写路径

- 默认写入 `ActiveMaster`（配置主或 Failover 选举后的新主）  
- 配置了 `DualWrite=true` 的从库，在启用双写时同步 fan-out（见 [双写](#7-双写)）

---

## 6. SQLBuilder 路由 API

所有方法返回 `this`，可链式调用；配置写入 `DBExecutor.RouteContext`（或执行前的 pending 上下文）。

| 方法 | 作用 |
|------|------|
| `useReadReplica()` | 显式标记本次读走从库 |
| `useMaster()` | 强制走主库（写路径） |
| `useTarget(position)` / `useTarget(instance)` | 指定连接位或实例 |
| `useReadPolicy(policy)` | 临时覆盖读策略 |
| `useFailover(mode)` | 临时覆盖 Failover 模式 |
| `useDualWrite(params int[] positions)` | 本次写同步 fan-out 到指定从库 |
| `useRoute(Action<SQLRouteContext>)` | 自定义配置路由上下文 |
| `resetRoute()` | 清除路由上下文 |

### 6.1 使用示例

```csharp
var db = cash.getInstance(0);

// 报表：临时走从库
db.useSQL().useReadReplica()
    .select("Id", "Amount").from("Order").query();

// 强一致读：强制主库
db.useSQL().useMaster()
    .select("*").from("Account").where("Id", id).queryRow<Account>();

// 指定连接位（如运维手动升主后的热备）
db.useSQL().useTarget(2)
    .select("*").from("User").query();

// 临时双写：仅本 insert 同步写主 + 从库 3、4
db.useSQL().useDualWrite(3, 4)
    .set("Name", "test").into("Log")
    .doInsert();

// 组合：从库读 + 信令触发异步复制（与路由正交）
db.useSQL().useReadReplica().useSignal("order-sync")
    .select("*").from("Order").query();

// copy 派生 Builder 会克隆 RouteContext
var sub = kit.copy();
```

::: warning 作用域
路由上下文随 `DBExecutor` 生命周期；不同 Builder 实例互不影响。`copy()` 会继承路由配置，`resetRoute()` 可清除。
:::

---

## 7. 双写

### 7.1 组级双写

```csharp
client.configureGroup(0, g => g
    .master(0)
    .enableDualWrite(3, 4));  // 等价于 addSlave + DualWrite + WriteEnabled
```

### 7.2 Builder 临时双写

```csharp
db.useSQL().useDualWrite(3, 4)
    .set("Col", val).into("Table")
    .doInsert();
```

启用双写时，框架会跳过该次执行的异步 `ModifyMediator` 复制（`SkipAsyncReplication`），避免重复投递。

### 7.3 失败策略（DualWriteErrorPolicy）

| 策略 | 行为 |
|------|------|
| `MasterWins`（默认） | 主库成功即视为成功；从库失败记录但不抛错 |
| `AllMustSucceed` | 任一从库失败则抛出异常 |

通过 `client.MasterSlaveOptions.DualWriteError` 配置。

::: warning
双写不保证分布式一致性，业务需自行处理幂等与冲突。
:::

---

## 8. 灾备切换（Failover）

### 8.1 Failover 模式

| 模式 | 行为 |
|------|------|
| `Disabled` | 不自动切换 |
| `MarkOnly` | 仅标记病态，不换连接 |
| `OnNextConnect`（推荐默认） | 主库不可用时，**下次写路径** `ExecuteCmd` 时选举热备 |
| `ImmediateOnFailure` | 当前 SQL 连接失败时立即选举并重试一次（无活动事务） |

### 8.2 选举规则

候选从库：`HotStandby=true` 且 `CanFailover`（健康可用）。

排序：连接位顺序（可扩展自定义 `CustomFailoverElector`）。

选举成功后：`group.ActiveMaster` 更新，触发 `MooClient.events.OnFailover`。

### 8.3 手动操作

```csharp
// 手动触发 Failover
var newMaster = client.tryFailover(0);

// 运维确认后手动回切（旧主恢复后不自动回切，避免双主）
client.promoteMaster(groupId: 0, masterPosition: 0, manual: true);
```

### 8.4 事务约束

- 已开启的事务绑定原连接，**不会在执行中途切换**  
- Failover 后**新事务**走新主  
- `ImmediateOnFailure` 默认仅对无活动事务的语句生效

---

## 9. 连接健康检查

每个 `DBInstance` 可挂载 `DBHealth`，在取实例与执行 SQL 时自动探活。

### 9.1 健康状态

| 状态 | 含义 |
|------|------|
| `None` | 未知，待首次探活 |
| `Available` | 可用 |
| `Unavailable` | 连续失败达阈值，暂不可用 |
| `Probing` | 恢复探测中 |

### 9.2 探活时机

| 机制 | 触发点 |
|------|--------|
| 懒探活 | `getInstance` 时，状态未知或超过 `StaleThreshold` |
| 执行前 | `ExecuteCmd` 入口，不可用实例抛 `DBUnavailableException` |
| 执行后 | 连接类异常 → `MarkFailure`；成功 → `MarkSuccess` |
| 定时探活 | `HealthProbeScheduler` 对 `Unavailable` 实例按 `RecoveryInterval` 周期探测 |

### 9.3 配置（DBHealthOptions）

可在 `DataBase.healthOptions` 中 per-库配置：

```csharp
new DBHealthOptions
{
    Enabled = true,
    MaxFailures = 3,              // 连续失败次数达阈值 → Unavailable
    ReTrySize = 10,               // 超过后停止定时探活
    RecoveryInterval = TimeSpan.FromSeconds(30),
    StaleThreshold = TimeSpan.FromMinutes(5),
    CustomPingSQL = "SELECT 1"     // 覆盖方言默认 ping SQL
};
```

方言默认 ping：MySQL `SELECT 1`，Oracle `SELECT 1 FROM DUAL` 等。

手动恢复：

```csharp
db.EnsureHealth().markManualRecovery();
```

---

## 10. Repository 与 SQLClip

路由上下文与事务采用相同传递模式：**状态在 Executor，上层只做传递**。

### 10.1 传递 Executor

```csharp
var kit = db.useSQL().useReadReplica();
kit.beginTransaction();  // 或任意 query/doUpdate，首次执行时会创建 Executor

var repo = db.useRepo<User>().useRoute(kit.Executor);
var clip = db.useClip<Order>().useRoute(kit.Executor);
```

`useRoute(executor)` 与 `useTransaction(executor)` 共用同一 `DBExecutor`，可并用：

```csharp
kit.beginTransaction();
repo.useRoute(executor).useTransaction(executor);
// ... 事务内操作
kit.commit();
```

### 10.2 直接配置 RouteContext

```csharp
db.useRepo<User>().useRoute(r => r.PreferReadReplica = true);
db.useClip<Order>().useRoute(r => r.ForceMaster = true);
```

---

## 11. 与异步复制（useSlave）配合

异步复制在 Builder 层注册，与读写路由独立：

```csharp
builder.useSlave(team => team
    .sign(0, replicaPositions)   // 主库连接位 → 从库列表
    .Signal = "order-sync");     // 可选信令模式

// 业务侧
db.useSQL()
    .useSignal("order-sync")     // 仅带该信令的 SQL 异步复制
    .set("Status", 1).from("Order").where("Id", id)
    .doUpdate();
```

| 场景 | 推荐 |
|------|------|
| 延迟可接受、从库仅跟随 | `useSlave` + 从库 `AsyncReplica=true` |
| 强一致双写 | 从库 `DualWrite=true` 或 `.useDualWrite()` |
| 灾备提升 | 从库 `HotStandby=true` + `FailoverMode` |

---

## 12. 事件监听

通过 `MooClient.events` 订阅：

```csharp
client.events.OnHealthStatusChanged += (db, oldStatus, newStatus) =>
{
    // 实例健康状态变更
};

client.events.OnFailover += ctx =>
{
    // ctx.OldMaster / ctx.NewMaster / ctx.Trigger
};
```

也可在 `MasterSlaveOptions.OnFailover` 中配置组级回调。

---

## 13. 常见问题

### Q1：注册主从组后，不写 `useReadReplica()` 也会走从库吗？

会。注册组后，`SELECT` 在 `ExecuteCmd` 内自动走 `ResolveRead`，按组的 `ReadPolicy` 选择从库。`useReadReplica()` 用于显式表达意图，或配合未注册组的场景。

### Q2：读从库会读到最新数据吗？

不一定。从库可能存在复制延迟。强一致读请使用 `.useMaster()` 或 `ReadRoutePolicy.MasterOnly`。

### Q3：Failover 后旧主恢复会自动切回吗？

不会。避免双主写入。请通过 `promoteMaster(..., manual: true)` 在运维确认后手动回切。

### Q4：事务内能否切换从库读？

不能。活动事务锁定 Session 所在实例，读从库与 Failover 均被忽略。

### Q5：如何监控当前 ActiveMaster？

```csharp
var group = client.getGroup(0);
var active = group?.GetActiveMaster();
```

---

## 14. 相关文档

- 内部设计说明：`doc/slave/主从与多库功能设计.md`
- 事务传递：[事务](/moohelp/morelv/transaction)
- 仓储：[SooRepository](/SQL/high/repository)
- SQLClip：[SQLClip](/SQL/high/sqlclip)
- 初始化配置：[初始化配置](/SQL/basis/initconfig)
