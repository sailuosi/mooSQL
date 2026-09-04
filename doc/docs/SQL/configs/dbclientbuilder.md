---
outline: deep
---

# BaseClientBuilder（流式配置）

mooSQL 用户侧顶级构建器。ext 包提供的 `DBClientBuilder` 继承本类并预注册方言 / 实体解析器。调用 `doBuild()` 返回可用的 `DBInsCash`。

::: tip 相关
- 连接位属性字典：[初始化配置](/SQL/basis/initconfig)
- 主从高级能力：[主从与多库](/SQL/high/masterslave)
:::

---

## 1. 完整起步案例

含义：用 JSON 连接位列表注册库；打开慢 SQL 与执行错误日志；注册主从全局默认；最后 `doBuild`。

````c#
var positions = configuration.GetSection("Connections").Get<List<DBPosition>>();

var cash = new DBClientBuilder()
    .useDataBase(positions)
    .useMasterSlave(o =>
    {
        o.DefaultReadPolicy = ReadRoutePolicy.WeightedRandom;
        o.ReadFallbackToMaster = true;
        o.DefaultFailover = FailoverMode.OnNextConnect;
    })
    .onWatchSlowSQL((ctx, elapsed, id) =>
    {
        // 仅当连接位 WatchSQL=true 且耗时 > MinTimeSpan 时触发
        logger.LogWarning("慢SQL {Ms}ms {Id}", elapsed.TotalMilliseconds, id);
    })
    .onExecuteError((ctx, ex, id) =>
    {
        logger.LogError(ex, "SQL失败 {Id}", id);
        return id; // 返回值可作操作标识透传
    })
    .onBeforeExecute((ctx, id) => id)
    .onAfterExecute((ctx, id) => id)
    .doBuild();

// 主从组（需已注册连接位 0/1/2）
cash.client.configureGroup(0, g => g
    .master(0)
    .autoReadReplica(true)   // 默认关闭；开启后 SELECT 才自动走从库
    .addSlave(1, s => { s.ReadReplica = true; s.Weight = 2; })
    .addSlave(2, s => { s.ReadReplica = true; s.HotStandby = true; s.WriteEnabled = true; }));

var db = cash.getInstance(0);
````

功能说明：`useDataBase` 把连接位写入实例池；`useMasterSlave` 设置全局策略；事件在构建期挂到 `MooClient.events`；`configureGroup` 在 build 后按连接位组成主从组。

---

## 2. doBuild

````c#
DBInsCash cash = builder.doBuild();
````

**含义**：应用已注册的方言工厂、Watchor、Logger、连接位等，创建/补全 `DBInsCash`，并把 `client` 挂到 cash。之后用 `cash.getInstance(pos)` / `cash.client`。

---

## 3. 连接与配置注册

### useDataBase（按连接位工厂）

````c#
builder.useDataBase(pos =>
{
    // 每次解析某连接位时调用；可懒加载、按环境切换
    return new DataBase()
        .setIndex(pos)
        .setDBType(DataBaseType.MySQL)
        .setConnection(GetConn(pos));
});
````

**含义**：注册「按 position 加载 `DataBase`」的委托，对应 `MooClient.useDataBase(Func<int,DataBase>)`。

### useDataBase（单连接位）

````c#
builder.useDataBase(1, () => new DataBase
{
    index = 1,
    name = "biz",
    dbType = DataBaseType.MSSQL,
    DBConnectStr = "...",
    watchSQL = true,
    minTimeSpan = 500,
    readable = true,
    writable = true
});
````

**含义**：只为指定连接位注册工厂，适合少量库。

### useDataBase（DBPosition 列表）

````c#
builder.useDataBase(new List<DBPosition>
{
    new DBPosition
    {
        Position = 1,
        Name = "biz",
        DbType = "MSSQL",
        ConnectString = "...",
        WatchSQL = true,
        MinTimeSpan = 500,
        Readable = true,
        Writable = false, // 只读
        CustomPingSQL = "SELECT 1",
        PingTimeoutMs = 3000
    }
});
````

**含义**：配置文件 / 代码列表一次注册；内部 `asDataBase` + `addConfig`。属性字典见 [初始化配置](/SQL/basis/initconfig)。

### useDBXMLConfig

````c#
builder.useDBXMLConfig(@"C:\app\bin\db.xml");
````

**含义**：设置 `DBInsCash.configPath`。加载配置时解析连接，并由 `MasterSlaveConfigLoader` 读取 `master`/`slave` 节点。详见 [主从 · XML](/SQL/high/masterslave#32-xml-配置)。

### useCashHolder

````c#
builder.useCashHolder(new MyCustomDBInsCash());
````

**含义**：使用自定义 `DBInsCash` 子类作为缓存持有者；一般无需调用。

---

## 4. 方言与实体

### useDialectFactory / useDialect

````c#
builder
    .useDialectFactory(new MyDialectFactory())
    .useDialect(DataBaseType.MySQL, () => new MySqlDialect());
````

**含义**：自定义或覆盖某类型方言的创建方式。未内置支持的库可通过工厂扩展。

### useEntityAnalyseFactory / useEntityAnalyser

````c#
builder
    .useEntityAnalyseFactory(factory)
    .useEntityAnalyser(new SqlSugarEntityAnalyser()); // 亦保留拼写 useEnityAnalyser
````

**含义**：实体元数据解析（表名、主键、列映射）。对接 SqlSugar / UCML 等实体体系时注册对应 Analyser。

### useEntityTranslate

````c#
builder.useEntityTranslate(t =>
{
    // 全局实体翻译钩子，见仓储实体翻译文档
});
````

**含义**：配置 `EntityTranslator` 全局行为（删除钩子等）。

### useClientFactory

````c#
builder.useClientFactory(new MyDBClientFactory());
````

**含义**：覆盖 `useSQL` / `useRepo` 等工厂创建逻辑。

### useSQLBuilderOption

````c#
builder.useSQLBuilderOption(opt =>
{
    opt.UpdateSetNullOpt = UpdateSetNullOpt.IgnoreNull; // 或 AsDBNull / None
});
````

**含义**：控制 UPDATE SET 遇到 null 时的策略。

---

## 5. 插件：Watchor / Logger / Cache

### useWatcher

````c#
builder.useWatcher(new QueryWatchor());
````

**含义**：注册 `IWatchor`，在命令执行前后做日志、改写等 AOP（与慢 SQL 事件不同通道）。

### useLogger

````c#
builder.useLogger(new FileExeLog());
````

**含义**：注册 `IExeLog`，持久化执行日志。

### useCache

````c#
builder.useCache(myCache);
````

**含义**：注册 `ISooCache`，供结果缓存等功能使用。

---

## 6. 主从与异步复制入口

### useMasterSlave

````c#
builder.useMasterSlave(o =>
{
    o.DefaultFailover = FailoverMode.OnNextConnect;
    o.DefaultReadPolicy = ReadRoutePolicy.WeightedRandom;
    o.ReadFallbackToMaster = true;
    o.DualWriteError = DualWriteErrorPolicy.MasterWins;
    o.OnFailover = ctx => Console.WriteLine(
        $"Failover {ctx.OldMaster?.config.index} -> {ctx.NewMaster?.config.index}");
});
````

**含义**：设置 `MooClient.MasterSlaveOptions` 全局默认。组级细节与路由 API 见 [主从与多库](/SQL/high/masterslave)。

### useSlave（异步复制插件）

````c#
builder.useSlave(team => team
    .sign(0, new[] { 1, 2 })  // 主连接位 0 → 异步投递到 1、2
    /* .Signal = "order-sync" */);
````

**含义**：主库 DML 成功后由 `ModifyMediator` **异步**复制到从库；与读写路由 / DualWrite **正交**。从库能力上常标 `AsyncReplica=true`。

---

## 7. 执行与构建生命周期事件

下列方法均链式返回 `this`，委托挂到 `MooClient.events`。每个案例下说明触发时机与用途。

### onBeforeExecute

````c#
builder.onBeforeExecute((ctx, operationId) =>
{
    // ctx.session / ctx.cmd 等已准备；可做审计埋点、改写标记
    return operationId; // 返回值作为后续链路中的操作标识
});
````

**含义**：SQL **即将执行**前调用。适合链路追踪、权限二次校验日志。不替代 `Readable`/`Writable` 闸门。

### onAfterExecute

````c#
builder.onAfterExecute((ctx, operationId) =>
{
    // 执行成功后；可记录影响行数、耗时辅助指标
    return operationId;
});
````

**含义**：执行**成功结束**后调用。

### onExecuteError

````c#
builder.onExecuteError((ctx, ex, operationId) =>
{
    var sql = ctx?.cmd?.cmdText;
    logger.LogError(ex, "SQL错误 {Sql}", sql);
    return operationId;
});
````

**含义**：执行抛错时调用。适合统一错误日志（含 SQL 文本）。异常仍会向上抛出（除非业务自行吞掉，一般不建议）。

### onWatchSlowSQL

````c#
builder.onWatchSlowSQL((ctx, elapsed, operationId) =>
{
    metrics.RecordSlowSql(elapsed, operationId);
});
````

**含义**：连接位 `WatchSQL=true` 且耗时超过 `MinTimeSpan` 时触发。参数为上下文、耗时、操作 Id。

### OnBeforeAddPara

````c#
builder.OnBeforeAddPara(paras =>
{
    // 参数集合写入命令前；可统一加租户、脱敏标记等
});
````

**含义**：绑定参数前触发（`event` 订阅）。

### onDBLiveCreated

````c#
builder.onDBLiveCreated(db =>
{
    Console.WriteLine($"实例已创建 pos={db.config?.index} name={db.config?.name}");
});
````

**含义**：`DBInstance` 构建完成时触发，可做健康挂载、诊断标签。

### onHealthChanged / onFailover

````c#
builder
    .onHealthChanged((db, oldStatus, newStatus) =>
    {
        // Available / Unavailable / Probing ...
    })
    .onFailover(ctx =>
    {
        alert.Send($"主从切换 {ctx.OldMaster?.config.index} -> {ctx.NewMaster?.config.index}");
    });
````

**含义**：健康状态变更与灾切选举成功时的回调。详见 [主从文档](/SQL/high/masterslave#12-事件监听)。

### onBuildSetFrag / onBuildWhereFrag

````c#
builder
    .onBuildSetFrag((frag, kit) =>
    {
        // INSERT/UPDATE 的 set 片段构建时；返回 false 可跳过该片段（视调用约定）
        // 常见：把 JValue 等动态类型转成字符串，避免驱动绑参失败
        return true;
    })
    .onBuildWhereFrag((frag, kit) =>
    {
        if (frag.paramed && frag.value is Newtonsoft.Json.Linq.JValue jv)
            frag.value = jv.ToString();
        return true;
    });
````

**含义**：SQLBuilder 拼装 SET / WHERE 值时的拦截。UCML / JSON 动态值场景常用。

### onCreatedSQL

````c#
builder.onCreatedSQL((sql, kit) =>
{
    // toSelect / toUpdate 等生成最终 SQL 字符串后
    Debug.WriteLine(sql);
});
````

**含义**：SQL **构建完成**（尚未执行或即将执行）时回调，便于调试输出。

---

## 8. 修改 SQL 审计（onSQLRuned）

````c#
builder
    .enableModifySqlAudit(true)
    .useModifySqlAuditSynchronous(false)      // false：异步派发
    .useChannelDispatchForModifySqlAudit(true) // 单消费者 Channel（推荐高并发）
    .includeInsertInModifySqlAudit(true)
    .includeCompositeInModifySqlAudit(false)
    .restrictModifySqlAuditToTables("Order", "OrderItem")
    .onSQLRuned(audit =>
    {
        // audit.Sql / RowsAffected / DbPosition / Parameters / Database
        auditStore.Save(audit);
    } /*, queryTypes: ..., targetTables: ... */);
````

**含义**：在删改（及可选 Insert/Composite）执行后投递 `SQLAuditContext`，用于合规审计。可用表名与语句类型过滤。进程退出前如启用 Channel，应调用 `client.ShutdownModifySqlAuditChannelDispatcher()`。

---

## 9. Schema / 分表 / 关系 / AOT（摘要）

````c#
builder
    .configureSchema(s =>
    {
        s.AllowSchemaSync = false;   // 禁止自动结构同步
        s.AllowDropColumn = false;   // 禁止删列
    })
    .useShard<Order>(o => $"Order_{o.CreateTime:yyyyMM}")
    .configureShard<Order>(cfg => { /* EntityShardConfig */ })
    .configureEntity<User>(e => { /* 导航关系 */ })
    .useAotMode(true)
    .registerGeneratedMaterializers(/* ... */);
````

**含义**：

| API | 作用 |
|-----|------|
| `configureSchema` | 全局 Schema 同步 / 删列闸门 |
| `useShard` / `configureShard` | 实体分表路由 |
| `configureEntity` | 导航关系 |
| `useAotMode` / `registerMaterializer*` | AOT 物化，减少反射 |

---

## 10. API 速查

| 类别 | 方法 |
|------|------|
| 构建 | `doBuild` |
| 连接 | `useDataBase`×3、`useDBXMLConfig`、`useCashHolder` |
| 方言/实体 | `useDialectFactory`、`useDialect`、`useEntityAnalyseFactory`、`useEntityAnalyser`、`useEntityTranslate`、`useClientFactory`、`useSQLBuilderOption` |
| 插件 | `useWatcher`、`useLogger`、`useCache` |
| 主从 | `useMasterSlave`、`useSlave` |
| 事件 | `onBeforeExecute`、`onAfterExecute`、`onExecuteError`、`onWatchSlowSQL`、`OnBeforeAddPara`、`onDBLiveCreated`、`onHealthChanged`、`onFailover`、`onBuildSetFrag`、`onBuildWhereFrag`、`onCreatedSQL` |
| 审计 | `onSQLRuned`、`enableModifySqlAudit`、`useModifySqlAuditSynchronous`、`useChannelDispatchForModifySqlAudit`、`includeInsertInModifySqlAudit`、`includeCompositeInModifySqlAudit`、`restrictModifySqlAuditToTables` |
| 其它 | `configureSchema`、`useShard*`、`configureShard`、`configureEntity`、`useAotMode`、`registerMaterializer*` |
