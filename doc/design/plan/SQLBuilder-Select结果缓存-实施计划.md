# 实施计划：SQLBuilder SELECT 结果缓存

> **归属**：业务层与查询体验 / SQL 特性（非富仓储）  
> **关联**：`doc/design/plan/业务层与查询体验-实施文档.md`（§5 原「`.expire` 统一」由本文取代）  
> **原则**：不新增 `.expire` API；显式 key 走既有 `setCache`；无外界 key 时用 `SQLCmd` 指纹；**仅缓存 SELECT 类查询结果**。  
> **修订**：2026-08-11

---

## 0. 目标与非目标

### 0.1 目标

1. 修通并产品化 **SQLBuilder / StepBuilder 查询结果缓存**（ResultCache）。  
2. **业务显式 key**：继续 `setCache(key, timeoutSeconds)`。  
3. **无外界 key**：在已启用结果缓存的前提下，用 **SQLCmd 稳定指纹** 作为 key。  
4. 自动指纹路径与写入路径均 **只作用于 SELECT 查询结果**。  
5. 与 ScriptTemplate 缓存、RichRepo EntityCache **严格分离**。

### 0.2 非目标

| 不做 | 说明 |
|------|------|
| 新增 `.expire` / Clip·LINQ 链上缓存管理 API | 管理入口只在 SQLBuilder（及 proxy） |
| 默认缓存一切 SELECT | 必须显式启用（有 key 或无 key 的 TTL 重载） |
| 缓存 INSERT/UPDATE/DELETE/MERGE 等写结果 | 禁止 |
| 把结果缓存下沉到 `SooRepository` | 薄仓不挂 |
| 替代 RichRepo `AllCache` / `QueryFromCache` | EntityCache 仍只在富仓储 |
| 改动 `useScriptTemplateCache` 语义 | 那是 SQL **生成**缓存，不是结果缓存 |

### 0.3 与富仓储边界

| | ResultCache（本文） | EntityCache（RichRepo） |
|--|---------------------|-------------------------|
| 入口 | `SQLBuilder.setCache` | `useRichRepo` 字典 API |
| 键 | 业务 key 或 SQLCmd 指纹 | `Type.FullName + DB` |
| 值 | 单次查询结果 | PK→实体字典 |
| 取数 | Builder `query*` | `CacheQuery().query` 仅取数，自管字典 |

RichRepo **可借助** ResultCache（对 `CacheQuery()` 调 `setCache`），但 **不**要求、也 **不**在 Clip 新增 expire。

---

## 1. 现状基线（代码事实）

| 项 | 现状 | 路径 |
|----|------|------|
| 缓存容器 | `ISooCache`：`Add` / `Get` / `Remove`；`Client.Cache` / `setCacheHolder` / 默认 `HashCache` | `ado/cache/` |
| 启用 API | `StepBuilder.setCache(string key, int timeout)`；`SQLBuilder.proxy` 已转发 | `StepBuilder.cs` |
| 读路径 | `query` / `query<T>` / `queryAsync*` / `queryPrepared*` / `queryFirst*` / `queryScalar*` 等在 **`cacheKey` 非空时 Get** | `StepBuilderDymatic.cs` |
| **写路径** | **缺失**：全库 Builder 侧 **无** `cacheHolder.Add(...)` | **本计划必须修复** |
| `cacheTimeout` | `setCache` 已写入字段，但从未用于 Add | 同上 |
| SQLCmd 指纹 | 无 | `SQLKit/SQLCmd.cs` |
| ScriptTemplate | `moo.st:` 前缀，与结果缓存键空间隔离 | `builder/cache/ScriptCacheKey.cs` |
| 单测 | 无 `setCache` 结果缓存测试 | — |

**结论**：现有能力是「半成品」——能声明 key、能读，**不能写入**；无 key 自动指纹未做。

---

## 2. API 设计（不引入 expire）

### 2.1 对外 API（仅 SQLBuilder / StepBuilder）

```csharp
// —— 已有：业务明确 key ——
kit.setCache(string key, int timeoutSeconds);

// —— 新增：无外界 key，启用 SELECT 结果缓存（指纹作 key）——
kit.setCache(int timeoutSeconds);

// —— 已落地：自动指纹键命名空间前缀（降碰撞）——
kit.useCachePrefix("Shop");           // → RC:Shop:{hashX8}
kit.useCachePrefix("report:daily");   // → RC:report:daily:{hashX8}
```

**决策（本计划采用）**

| 调用 | 行为 |
|------|------|
| `setCache("myKey", 300)` | `cacheKey =` 规范化用户键；`resultCacheEnabled = true`（**不受** `useCachePrefix` 影响） |
| `setCache(300)` | `cacheKey` 空；`resultCacheEnabled = true`；执行前用 `BuildAutoResultCacheKey(cmd)` |
| `useCachePrefix(prefix)` | 写入 `cacheKeyPrefix`；自动键 = `ComposeAutoCacheKeyPrefix` + `SQLCmd` 的 `X8` |
| 未调用 `setCache` | **不缓存**（与今日默认一致） |
| `clear()` | 清空 SQL 状态 **以及** `cacheKey` / `resultCacheEnabled` / `cacheKeyPrefix` |
| `runBuild` | 仅 `resetForOrchestrationReplay()`（不清缓存配置），否则会冲掉已设的 `setCache` |

**不提供**：`expire(TimeSpan)`、Clip `.expire`、LINQ `Expire`（本迭代不做；若业务要用结果缓存，对底层 Builder `setCache`）。

### 2.2 用户键规范化

显式 key 存入 `ISooCache` 前规范化，避免与模板键冲突：

```text
RC:USER:{databaseId}:{userKey}
```

- `databaseId`：优先 `DBLive.config` 可区分串库的稳定标识（如 index / 连接名，实现时定一种并写死文档）。  
- 若调用方 key 已以 `RC:` 开头，不再重复加前缀（或文档要求勿自加）。

### 2.3 TimeSpan

首期 **不**新增 `setCache(key, TimeSpan)` 也可；若加，仅作秒数重载糖，**不是** expire 语义。TTL 内部统一为 `int` 秒，对接 `ISooCache.Add(key, value, seconds)`。

---

## 3. SQLCmd 指纹（无外界 key）

### 3.1 职责（已落地于 `SQLCmd`）

指纹与模版编排共用 **`ScriptHash`**（net6+ 内部 `HashCode.Combine`；旧 TFM 确定性混洗），**不**另造 SHA256。

```csharp
// pure/src/ado/builder/SQLKit/SQLCmd.cs
cmd.EnsureLiveParasResolved();  // 执行前 / 取键前：ResolveDelayParas → 写回 sql
cmd.GetHashCode();              // ScriptHash：sql + 各参数名/值（键 Ordinal 排序）
cmd.GetCacheKey();              // "RC:" + GetHashCode().ToString("X8")
```

用户显式键仍可由上层规范化为 `RC:USER:{db}:{key}`（见 §2.2）；自动键直接用 `GetCacheKey()`。

可选：`ResultCacheKey.ForUser` 工具类仍可补，与 `SQLCmd.GetCacheKey` 并存。

### 3.2 指纹输入

| 分量 | 规则 |
|------|------|
| `sql` | **先** `EnsureLiveParasResolved()`，再对最终文本 `ScriptHash.Add` |
| 参数 | 参数名 Ordinal 排序；逐项 `Add(name)` + `Add(val)`（与编排 `ScriptHash.Add(object)` 一致） |
| 字符串键 | `RC:` + `(uint)GetHashCode()` 的 `X8`（对齐 `ScriptCacheKey` 编排哈希展示） |

`resultTypeTag` / `databaseId`：若需防串返回类型或串库，由 **调用方** 在 `GetCacheKey` 结果上再拼接（如 `GetCacheKey() + ":dt"`），首期查询钩子实现时定。

### 3.3 LivePara

`GetHashCode` / `GetCacheKey` / 执行前均走 `EnsureLiveParasResolved()` → `Paras.ResolveDelayParas`（幂等）。

### 3.4 SELECT 判定

仅当满足时才允许读/写结果缓存（自动指纹与「仅 SELECT」约束）：

```text
cmd.type == QueryType.Select
```

- `toSelect()` 产物应为 `Select`。  
- 手写 `exeQuery(SQLCmd)`：若 `type` 为 `Unknown`，**默认不按自动指纹缓存**；显式用户 key 是否允许由实现选择——**本计划建议：显式 key 也仅在 Select 时写入**，避免误缓存写语句。  
- `doUpdate` / `doInsert` / `doDelete` / `merge` 等路径：**永不**走结果缓存 Add。

---

## 4. 执行流程（核心）

### 4.1 状态字段（StepBuilder）

| 字段 | 含义 |
|------|------|
| `cacheKey` | 用户原始/已规范化键；空表示无外界 key |
| `cacheTimeout` | TTL 秒 |
| `resultCacheEnabled` | 是否启用结果缓存（`setCache` 任一重载置 true） |

（可用「`resultCacheEnabled || !string.IsNullOrEmpty(cacheKey)`」推导，但显式标志更清晰。）

### 4.2 统一钩子（避免 N 处复制）

在 `StepBuilderDymatic`（或新建 `StepBuilder.resultCache.cs`）抽取：

```csharp
bool TryGetResultCache<T>(string resultTypeTag, out T value);
void TryAddResultCache<T>(SQLCmd cmd, string resultTypeTag, T value);
string ResolveResultCacheKey(SQLCmd cmd, string resultTypeTag); // 用户键 or 指纹
bool CanUseResultCache(SQLCmd cmd); // enabled + Select + 非事务策略等
```

所有 `query*` / `queryPrepared*` / `queryFirst*` / `queryScalar*` / `queryAsync*` 走钩子；**禁止**再散落半截 Get。

### 4.3 有外界 key

```text
setCache(userKey, ttl)
  → resultCacheEnabled=true, cacheKey=userKey

query*:
  key = ForUser(db, userKey)  // 规范化可惰性
  if Get hit → return
  cmd = toSelect() / prepared
  if !CanUseResultCache(cmd) → 执行但不 Add
  执行
  Add(key, result, ttl)
```

说明：有用户 key 时，**允许在 toSelect 前 Get**（键与 SQL 无关，与现逻辑兼容）。

### 4.4 无外界 key（自动指纹）

```text
setCache(ttl)
  → resultCacheEnabled=true, cacheKey=""

query*:
  cmd = toSelect() / 使用 prepared SQLCmd
  确保 Delay 可解析或已解析
  if !CanUseResultCache(cmd) → 仅执行
  key = ForCommand(cmd, db, resultTypeTag)
  if Get hit → return
  执行
  Add(key, result, ttl)
```

**顺序变化**：自动路径必须先物化 `SQLCmd` 再 Get（与今日「先 Get 再 toSelect」不同）。

### 4.5 门面 SQLBuilder

- `setCache` 重载挂 `SQLBuilder.proxy` → `_inner`。  
- 查询仍经现有 proxy/query 路径进入内核；钩子在 StepBuilder 即可覆盖。  
- `queryPrepared` 热路径：用已有 `SQLCmd` 做指纹/写入，与 ScriptTemplate **并存**（先结果缓存键，未命中再执行；模板缓存仍只加速 SQL 生成）。

---

## 5. 策略（首期默认）

| 场景 | 策略 | 备注 |
|------|------|------|
| 未启用结果缓存 | 不读不写 | 默认 |
| 非 `QueryType.Select` | 不读不写 | 硬规则 |
| 事务中（`Executor` 已绑事务 / LiveTransaction 活动） | **默认跳过** 读与写 | 避免脏读进缓存；可后续 Options 放开 |
| 分页（`pageIndex>0` 或 `skip>0`） | **默认跳过** | 与 CRL 一致；**显式用户 key 时允许缓存**并在 XML 注释警告 |
| `queryPaged` | 默认不缓存整页组合 | 或仅当显式 key；首期建议跳过 |
| `count()` 单独调用 | 若走 Select 且已启用，可缓存 | `resultTypeTag` 区分 `count` |
| 空结果（null / 空表） | **缓存空结果** | 防穿透；TTL 同配置 |
| `clear()` 之后 | 本实例不再使用旧 key | 与现 clear 一致 |

策略开关（可选二期，首期写死默认即可）：

```csharp
// 二期：ResultCacheOptions { SkipInTransaction, SkipPagingWithoutUserKey }
```

---

## 6. 文件与改动清单

```text
pure/src/ado/builder/cache/
  ResultCacheKey.cs          # NEW：用户键 / AUTO 指纹
  （可选）ResultCacheScope.cs # 策略判断

pure/src/ado/builder/
  StepBuilder.cs             # setCache(int)；resultCacheEnabled；clear 联动
  StepBuilder.resultCache.cs # NEW：TryGet / TryAdd / CanUse（推荐）
  StepBuilderDymatic.cs      # query* 改为走钩子 + Add
  SQLBuilder.proxy.cs        # 转发 setCache(int)

pure/src/ado/builder/SQLKit/
  SQLCmd.cs                  # 可选：实例方法委托 ResultCacheKey

测试（建议 Tests/TestFast 或现有 Builder 测试工程）：
  ResultCache_UserKey_RoundTrip
  ResultCache_AutoFingerprint_SelectOnly
  ResultCache_Skip_NonSelect
  ResultCache_Skip_Transaction
  ResultCache_Paging_DefaultSkip_UserKeyOverride
```

**不改**：`richRepo/EntityCacheStore`、Clip 公开 API、LINQ Expire。

---

## 7. 与其它缓存的键空间

| 前缀 | 用途 |
|------|------|
| `moo.st:` | ScriptTemplate（已有） |
| `RC:USER:` | 结果缓存·业务 key |
| `RC:AUTO:` | 结果缓存·SQLCmd 指纹 |
| EntityCache 内部键 | RichRepo 自有（非 ISooCache 或另约定） |

禁止混用前缀。

---

## 8. 任务拆分

| 序号 | 任务 | 预估 | 状态 |
|:----:|------|:----:|:----:|
| R0 | 修复写路径：`TryAddResultCache` + 所有 query* 挂钩 | 0.5d | ✅ |
| R1 | `ResultCacheKey.ForUser` + 键规范化 | 0.25d | ✅ |
| R2 | `setCache(int)` + `resultCacheEnabled` + clear | 0.25d | ✅ |
| R3 | SQLCmd 指纹（ScriptHash） | 0.75d | ✅ |
| R4 | 自动路径：先 toSelect 再 Get/Add；仅 Select | 0.5d | ✅ |
| R5 | 事务跳过 + 分页默认跳过（显式 key 可覆盖分页） | 0.5d | ✅ |
| R6 | proxy 转发 + `useCachePrefix` | 0.25d | ✅ |
| R7 | 单测集 + 文档 | 0.5d | ✅ |

**合计约 3～3.5d。**

原业务层文档 C1–C5（expire / Clip / LINQ）**取消或降级**为「不采纳」；以 R0–R7 为准。

---

## 9. 验收用例

| # | 场景 | 期望 |
|---|------|------|
| 1 | `setCache("k", 60)` → `query()` 两次 | 第二次命中；`Add` 使用 timeout=60 |
| 2 | `setCache(60)` → 相同 Select 两次 | 指纹相同，第二次命中 |
| 3 | `setCache(60)` → 改 where 参数再查 | 指纹变，未命中 |
| 4 | `setCache(60)` 后走 update/doUpdate | **不**写入结果缓存 |
| 5 | 事务内 `setCache` + query | 默认不读不写 |
| 6 | 分页无用户 key | 默认不缓存 |
| 7 | 分页 + 显式用户 key | 可缓存（注释已警告） |
| 8 | `DataTable` 与 `query<T>` 同 SQL | 不同 `resultTypeTag`，互不覆盖 |
| 9 | 未 `setCache` | 行为与改造前一致 |
| 10 | 与 `useScriptTemplateCache` 同时开 | 互不破坏；键前缀不冲突 |

---

## 10. 风险与约束

1. **先修 Add**：否则任何 API 都是空转。  
2. **DelayPara**：指纹时机错误会导致错命中；解析失败则跳过缓存。  
3. **大结果集**：TTL 缓存可能撑爆内存；文档建议业务对大查询用显式短 TTL 或不用缓存。  
4. **可变实体列表**：缓存的是查询当下快照；RichRepo 写路径不会自动清 ResultCache（除非业务 `Remove` 或短 TTL）。  
5. **主从延迟**：读副本结果进缓存可能陈旧——写入文档，与 EntityCache 一致提醒。  
6. **不把管理 API 扩到 Clip**：Clip 若需缓存，调用 `Context.Builder.setCache(...)`（可选后续一行糖，非本计划必做）。

---

## 11. 目标用法（验收示意）

```csharp
// 业务明确 key
var rows = db.useSQL()
    .setCache("report:order:daily", 300)
    .select("Id, Amt")
    .from("Orders")
    .where("Dt", day)
    .query();

// 无外界 key：指纹缓存（仅 Select）
var list = db.useSQL()
    .setCache(300)
    .select("*")
    .from("SysDict")
    .where("Enabled", 1)
    .query<SysDict>();

// 富仓储字典缓存（另一条线，不在本计划实现）
var name = db.useRichRepo<SysDict>()
    .QueryItemFromCache(x => x.Code == "A")?.Name;
```

---

## 12. 对业务层实施文档的修订要求（本文落地后）

在 `业务层与查询体验-实施文档.md`：

- §0.1「SQL 特性」：将 `.expire` 改为「`setCache` + 可选 SQLCmd 指纹（见本文）」。  
- §5：整节改为摘要 + **链接本文**；删除 Clip/LINQ expire 任务。  
- §10 阶段 A：C1–C5 替换为 R0–R7。  
- §12 示例：去掉 `.expire`，改为 `setCache`。

（可在实施 R7 时一并改文档。）

---

## 13. 一句话

> **不新增 expire；`setCache(key,ttl)` 修通读写，`setCache(ttl)` 用 SQLCmd 指纹；只缓存 SELECT 结果——与 ScriptTemplate、RichRepo 字典缓存三分开。**

---

## 14. 实施开关

| 状态 | 说明 |
|------|------|
| 计划 | ✅ 本文 |
| 编码 | ✅ R0–R7 已落地（2026-08-11） |
| 文档回写业务层 §5 | ✅ 已指向本文 |

### 落地摘要

| 项 | 路径 |
|----|------|
| 钩子 / `setCache(int)` / 策略 | `StepBuilder.resultCache.cs` |
| `query*` 读写接通 | `StepBuilderDymatic.cs` |
| `runBuild` 不冲掉 setCache | `resetForOrchestrationReplay()`（非 `clear()`） |
| 指纹 / 前缀 | `SQLCmd.GetCacheKey` / `useCachePrefix` |
| 用户键 | `ResultCacheKey.ForUser` |
| 单测 | `Tests/TestBug/.../SQLBuilderResultCacheTests.cs`（8） |
