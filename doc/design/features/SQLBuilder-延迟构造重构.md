# SQLBuilder 延迟构造重构设计

> 面向 **mooSQL 项目开发人员**。说明将 SQLBuilder「编排（API 调用）」与「构造（状态物化）」剥离的目标、架构、分阶段任务与风险边界。  
> 用户侧链式 API 保持不变；本文不替代 [SQLBuilder 使用文档](../../docs/SQL/basis/SQLBuilder.md)。

---

## 1. 背景与目标

### 1.1 现状问题

当前 SQLBuilder 采用 **eager structured state + late dialect render**：

```
public API（select/from/where/…）
    → 立即改写 SqlGoup / WhereCollection / CTE / Union / Paras
    → toXxx / queryXxx / doXxx
        → SqlGoup.build* → FragSQL → SQLExpression → SQLCmd
```

问题集中在三点：

| 问题 | 表现 |
|------|------|
| **职责纠缠** | 同一套 public 方法既承担「DSL 编排」又承担「片段构造」；`SQLBuilderSelect` / `Where` / `Save` 与 `SqlGoup` 紧耦合 |
| **中间态难复用** | 调用链过程中已形成方言相关字符串、参数名、嵌套 `toSelect()` 结果，难以做跨库重放、AOT/脚本化、统一调试 |
| **Apart 半成品** | 已有 `record` / `toApart` / `useApart`，但是「可选 where 步骤 + 事后快照 select/from」，不是全量「调用即入队、执行前构造」 |

核心文件体量（约）：`SQLBuilderWhere.cs` ~1.6k、`SQLBuilderDymatic.cs` ~1.3k、`SQLBuilder.cs` ~0.8k、`SQLBuilderSelect.cs` ~0.5k，改造需分阶段，避免一次性大爆炸。

### 1.2 设计目标

| 目标 | 说明 |
|------|------|
| **编排 / 构造分离** | 新 `SQLBuilder` 只做编排入队；原实现整体更名为 `StepBuilder`，专司构造与执行 |
| **现有实现零改逻辑** | 改造启动时 **不改动** 原 `SQLBuilder` 方法体；仅类型更名，行为原样保留 |
| **调用即入队** | 新门面每个 public **非执行** 方法：`new XxxStep(args)` 推入 `IStep` 队列 |
| **一方法一 Step** | 每个需编排的 public 方法对应一个实现 `IStep` 的类 |
| **执行前物化** | 在 `toXxx` / `queryXxx` / `doXxx` 前，按序 `Apply` 到内建 `StepBuilder`，再委托其既有出口 |
| **对外兼容** | 类型名仍为 `SQLBuilder`；链式 API 签名与语义不变 |

### 1.3 非目标（本期不做）

- 不改变 Dialect / `SQLExpression.build*` 的 SQL 拼装算法本身  
- 不重构 Repository / SQLClip / Ext LINQ 的对外 API（仅适配其调用 SQLBuilder 的物化时机）  
- 不做跨 `DataBaseType` 的 Apart 复用  
- 不引入源码生成 / IL 织入作为硬依赖（可用轻量手写 Step，后续再考虑 codegen）

### 1.4 成功判据

1. 构造链路上，`select` / `where` / `set` 等在「仅链式调用、未触发物化」时 **不** 修改 `SqlGoup` 业务片段（或仅写入队列）。  
2. 任意 `toSelect` / `doUpdate` / `query<T>` 等与改造前产出 **等价 SQLCmd**（SQL 文本 + 参数键序在既有测试约定下一致）。  
3. Apart：`toApart` / `useApart` 可建立在同一队列模型上，不再依赖「事后从列表反推步骤」的主路径。  
4. 嵌套 `Action<SQLBuilder>`、UNION、CTE、Merge 场景有明确物化规则且有回归用例。

---

## 2. 概念定义

| 术语 | 含义 |
|------|------|
| **SQLBuilder（新）** | 对外编排门面：实现原全部 public API；构造类方法只入队；执行类方法先 Flush 再委托 |
| **StepBuilder（原 SQLBuilder）** | 由现类型整体更名而来；**方法体初期完全不动**；承接 Flush 回放与全部构造/执行逻辑 |
| **编排（Orchestration）** | 新 `SQLBuilder` 上的 DSL：入队、门控、嵌套闭包、元操作 |
| **构造（Construction）** | `StepBuilder` 内对 `SqlGoup` / where / CTE / Union / `Paras` 的既有改写 |
| **执行（Execution）** | `StepBuilder` 的 `query*` / `do*` / `exe*` |
| **IStep** | 编排步骤抽象；一次 public 构造（或需入队的）调用对应一个实现类 |
| **步骤队列** | `SQLBuilder` 内建 `List<IStep>`（或等价结构）；可切片为 `SQLApart` |
| **物化（Flush）** | 按序 `step.Apply(stepBuilder)`，把队列回放到内建 `StepBuilder` |
| **非执行 public 方法** | 返回 `SQLBuilder`（或嵌套 DSL）的构造/配置方法；**排除** `toXxx` / `queryXxx` / `doXxx` / `count` / `exist` 等 |

---

## 3. 现状架构摘要

### 3.1 模块位置（改造前）

| 路径 | 职责 |
|------|------|
| `pure/src/ado/builder/SQLBuilder*.cs` | 对外编排 + eager 构造（将更名为 `StepBuilder`） |
| `pure/src/ado/builder/SQLKit/SqlGoup.cs` | 单语句片段袋 + `buildSelect` 等 |
| `pure/src/ado/builder/SQLKit/WhereCollection.cs` | where 树；可选 `Stepable<WhereStep>` |
| `pure/src/ado/builder/apart/*` | Apart 快照 / 重放 |
| `pure/src/ado/builder/step/Stepable.cs` | 通用录制开关（where 用） |
| `pure/src/ado/data/dialect/SQLExpression*.cs` | 方言层最终 SQL |

### 3.2 与 Apart 的关系（差距）

```
现状 Apart:
  record() → 仅打开 wherePart.steps.recordNow
  API 调用 → 仍立即改 SqlGoup
  toApart() → ApartEmitter 从「已构造状态」反推 IApartStep
  useApart() → 再调 public API（再次 eager）

目标:
  新 SQLBuilder 构造 API → enqueue(IStep)
  toXxx/queryXxx/doXxx → Flush → StepBuilder 既有 to*/query*/do*
  Apart ≈ IStep 队列切片（一等公民）
```

---

## 4. 改造实施方式（锁定）

> 本节为实施主路径，优先于旧「原地改 SQLBuilder + DualWrite」方案。

### 4.1 总原则

```
1. 现有 SQLBuilder 全员不动（方法体 / SqlGoup 协作逻辑保持原样）
2. 类型更名：SQLBuilder → StepBuilder（partial 全套同步更名）
3. 新建类型：SQLBuilder（编排门面），实现原全部 public 方法（先空实现/桩）
4. 门面内建 IStep 队列，维护编排步骤
5. 定义 IStep；为每一个需编排的 public 方法建立一个实现类
6. 构造方法：入队后 return this；执行方法：Flush 后委托内建 StepBuilder
```

**禁止**在 Phase-1/2 去「改一点点原方法体做双写」——原实现整体冻结为 `StepBuilder`，差异只发生在新门面与 Step 类中。

### 4.2 类型更名：`SQLBuilder` → `StepBuilder`

| 项 | 做法 |
|----|------|
| 类型名 | `public partial class SQLBuilder` → `public partial class StepBuilder` |
| 文件名 | 建议同步：`SQLBuilder.cs` → `StepBuilder.cs`，`SQLBuilderWhere.cs` → `StepBuilderWhere.cs`，…（或暂留文件名、只改类型，二选一；推荐文件名同步以免混淆） |
| 方法体 | **零逻辑改动**；仅因类型改名产生的 `SQLBuilder` → `StepBuilder` 符号替换 |
| 可见性 | 建议 `internal`（应用层只认新 `SQLBuilder`）；若测试需直连可暂 `public` + `[Obsolete]` |
| 内部自引用 | `getBrotherBuilder` / `copy` / `new SQLBuilder()` 等改为 `new StepBuilder()` / 返回 `StepBuilder` |
| 被引用处 | `SqlGoup.root`、Apart、SQLClip、扩展方法等：构造执行宿主指向 `StepBuilder`；**对外 API 宿主**指向新 `SQLBuilder` |

更名检查清单（工具辅助全局替换后人工复核）：

- [ ] `partial class` 全部分片  
- [ ] 构造函数、返回类型、字段类型中的自引用  
- [ ] `Apart*` / `WhereStep.Replay` 中对 Builder 的参数类型（Flush 路径应接受 `StepBuilder`）  
- [ ] 测试若直接 `new SQLBuilder()`：改为 `useSQL()` 或新门面构造  

### 4.3 新建门面：`SQLBuilder`

新建独立类型（**不要**再做成 `StepBuilder` 的 partial）：

```csharp
namespace mooSQL.data
{
    /// <summary>
    /// SQL 编排门面：构造 API 只记录 IStep；执行前 Flush 到内建 StepBuilder。
    /// </summary>
    public partial class SQLBuilder : IDisposable
    {
        private readonly List<IStep> _steps = new List<IStep>();
        private readonly StepBuilder _inner;   // 构造/执行宿主，初期即完整旧实现
        private bool _dirty = true;           // 队列变更后需重新 Flush

        public SQLBuilder() { _inner = new StepBuilder(); /* 同步必要配置 */ }
        public SQLBuilder(DBInstance db) { /* setDBInstance 等到 _inner */ }

        internal IReadOnlyList<IStep> Steps => _steps;
        internal StepBuilder Inner => _inner;

        private SQLBuilder Enqueue(IStep step)
        {
            _steps.Add(step);
            _dirty = true;
            return this;
        }

        internal void EnsureMaterialized()
        {
            if (!_dirty) return;
            _inner.clear(); // 或约定：从干净状态重放
            foreach (var step in _steps)
                step.Apply(_inner);
            _dirty = false;
        }
    }
}
```

**空方法阶段（骨架里程碑）**：

1. 用反射或 API 清单，为原 `SQLBuilder` **每一个 public 实例方法/属性** 在新类上生成同签名成员。  
2. 构造类方法体：`throw new NotImplementedException()` 或 `return Enqueue(new XxxStep(...));`（参数先收齐）。  
3. 执行类方法体：暂 `EnsureMaterialized(); return _inner.Xxx(...);`（骨架期即可委托，便于早期冒烟）。  
4. 公共属性（`DBLive` / `ps` / `Dialect`…）：默认 **透传到 `_inner`**，保证扩展与调试可读。

> 「空方法」指门面尚无完整业务逻辑，不是永久 `throw`；骨架合并后应尽快改为标准入队/委托模板。

### 4.4 `IStep` 与「一方法一类型」

```csharp
namespace mooSQL.data.builder.steps
{
    /// <summary>
    /// 编排步骤：携带一次 public API 调用的参数，在 Flush 时作用于 StepBuilder。
    /// </summary>
    public interface IStep
    {
        /// <summary>将本步骤应用到构造宿主（原实现）。</summary>
        void Apply(StepBuilder builder);
    }
}
```

规则：

| 规则 | 说明 |
|------|------|
| **一方法一 Step 类** | 例如 `select(string)` → `SelectStep`；`where(string, object, string, bool)` → `WhereKeyValOpParamedStep`（命名见下） |
| **重载 = 不同类型** | 不同参数列表各自一类，避免一个类塞多个工厂导致 Apply 分支失控 |
| **载荷不可变** | 构造时捕获参数；`Action<SQLBuilder>` 在编排期执行并捕获**子门面的步骤列表**（或子 `SQLBuilder`），勿在 Apply 时再执行会改外部状态的不确定逻辑 |
| **Apply 只打 StepBuilder** | `Apply` 内调用 `_inner.select(...)` 等 **StepBuilder public/internal API**；禁止 `Apply` 再调新 `SQLBuilder` 入队（防重入） |
| **执行方法不建 Step** | `toSelect` / `query` / `doUpdate` 等不入队，只 Flush + 委托 |

命名建议：

```
{ApiName}[OverloadHint]Step

select(string)              → SelectStep
select(string, Action<…>)   → SelectSubqueryStep
where(string, object)       → WhereKeyValStep
where(string, object, string, bool) → WhereKeyValOpParamedStep
leftJoin(string)            → LeftJoinStep
setPage(int?, int?)         → SetPageStep
```

目录建议：`pure/src/ado/builder/steps/`，可按家族分子目录 `select/` `where/` `set/` `union/` `cte/`…

### 4.5 门面方法标准模板

**构造（入队）**

```csharp
public SQLBuilder select(string columns)
{
    return Enqueue(new SelectStep(columns));
}

// steps/SelectStep.cs
sealed class SelectStep : IStep
{
    private readonly string _columns;
    public SelectStep(string columns) => _columns = columns;
    public void Apply(StepBuilder builder) => builder.select(_columns);
}
```

**执行（Flush + 委托）**

```csharp
public SQLCmd toSelect()
{
    EnsureMaterialized();
    return _inner.toSelect();
}

public IEnumerable<T> query<T>()
{
    EnsureMaterialized();
    return _inner.query<T>();
}
```

**元操作（立即生效，通常不入队或特殊步骤）**

```csharp
public SQLBuilder clear()
{
    _steps.Clear();
    _inner.clear();
    _dirty = false;
    return this;
}
```

**配置透传（可不入队）**

```csharp
public SQLBuilder setDBInstance(DBInstance db)
{
    _inner.setDBInstance(db);
    return this;
}
```

### 4.6 目标分层（实施后）

```
应用 / 扩展方法 / SQLClip
        │
        ▼
┌───────────────────────────────────────┐
│  SQLBuilder（新门面）                  │
│  - List<IStep> 编排队列                │
│  - 构造 API → Enqueue                  │
│  - to/query/do → Flush → 委托          │
└───────────────────┬───────────────────┘
                    │ Apply 回放
                    ▼
┌───────────────────────────────────────┐
│  StepBuilder（原 SQLBuilder，逻辑冻结） │
│  - SqlGoup / Where / CTE / Union / ps  │
│  - 既有 toXxx / queryXxx / doXxx       │
└───────────────────┬───────────────────┘
                    ▼
         Dialect / SQLExpression → SQLCmd
```

### 4.7 调用时序

```
kit.select("id").from("t").where("id", 1).query<T>()

1) select  → _steps += SelectStep("id")       // 不碰 StepBuilder 片段
2) from    → _steps += FromStep("t")
3) where   → _steps += WhereKeyValStep(...)
4) query   → EnsureMaterialized()
              _inner.clear()
              SelectStep.Apply(_inner) → _inner.select("id")
              FromStep.Apply(_inner)   → _inner.from("t")
              Where….Apply(_inner)     → _inner.where(...)
           → return _inner.query<T>()
```

### 4.8 物化触发点

| 类别 | 入口 |
|------|------|
| **主出口** | 新 `SQLBuilder` 的全部 `toXxx` |
| **执行包装** | `query*` / `do*` / `count` / `exist` / `checkExistKey` / `exe*`（若依赖已编排状态） |
| **读中间态** | `ColumnCount` / `ConditionCount` / `containSetColumn` / `buildWhere*` —— getter/方法内先 `EnsureMaterialized()` |
| **嵌套** | 父步骤 `Apply` 时物化子脚本（子 `SQLBuilder` Flush 或直接把子 `IStep` 应用到兄弟 `StepBuilder`） |
| **Apart** | `toApart` 导出 `_steps`；`useApart` 追加到 `_steps` |

### 4.9 嵌套 `Action<SQLBuilder>`（门面语义）

```csharp
public SQLBuilder select(string asName, Action<SQLBuilder> doColSelect)
{
    var child = CreateChildFacade(); // 共享 _inner.ps / seed 策略与 StepBuilder.getBrotherBuilder 对齐
    doColSelect(child);
    return Enqueue(new SelectSubqueryStep(asName, child.Steps)); // 或持有 child 引用
}
```

`SelectSubqueryStep.Apply(StepBuilder parentInner)`：按旧实现等价方式创建 brother `StepBuilder`、回放子步骤、`toSelect()`、写入父 `selectPart`。  
初期允许 Step 内 **直接复刻** 旧 `SQLBuilderSelect.select(string, Action)` 的控制流，只要参数来自已捕获的子步骤而非再次调用会入队的门面。

### 4.10 分阶段实施节奏

| 阶段 | 内容 | 完成标志 |
|------|------|----------|
| **P0** | API 清单（全部 public 方法/属性/事件）；回归测试基线 | 清单入库 |
| **P1** | 全局更名 → `StepBuilder`；编译通过；测试临时改指向或仍测 StepBuilder | 旧行为绿 |
| **P2** | 新建 `SQLBuilder` + `IStep`；全部 public **桩签名**；`useSQL()` 返回新门面 | 编译通过 |
| **P3** | 逐家族实现 Step 类 + 入队；执行方法 Flush+委托；无嵌套 API 先绿 | 核心查询/更新测试绿 |
| **P4** | 嵌套闭包 / UNION / CTE / Merge / Apart | 全量 SQLBuilder* 测试绿 |
| **P5** | `StepBuilder` 收为 `internal`；Apart 与旧 Emitter 收敛；文档/Skills | 发布就绪 |

对比旧 DualWrite 方案：本方式用「门面 vs 冻结内核」代替「同一类双写」，语义对比测试可写成：

```text
同一调用链 → 新 SQLBuilder.toSelect()  vs  纯 StepBuilder 手写链.toSelect()
断言 SQL + 参数键一致
```

---

## 5. API 分类与语义约定

### 5.1 记录入队（Record-only）

以下在被调用时 **只入队**（或入队 + 极轻量编排状态），不调用 Dialect，不写入片段字符串到 `SqlGoup`：

- CTE：`withSelect` / `withAs` / `withRecur*`  
- SELECT：`select*` / `distinct` / `top` / `skip*` / `take` / `skipTake` / `setPage` / `rowNumber*` / `selectSummary`…  
- FROM/JOIN：`from*` / `join*` / `leftJoin` / `innerJoin` / `rightJoin` / `pivot` / `unpivot`  
- GROUP/ORDER：`groupBy` / `having` / `orderBy`  
- UNION：`union*` / `unionAll` / `toggleToUnionOutor`  
- WHERE：全部 `where*` / `sink*` / `rise` / `and` / `or` / `pin*` / `clearWhere`  
- SET：`setTable` / `set*` / `newRow` / `addRow`…  
- MERGE DSL 配置方法、`prefix` / `subfix`、条件片段相关构造  
- `ifs`：推荐编排期门控，被跳过的调用 **不入队**（见 §5.4）

### 5.2 物化 + 产出 / 执行（不入「构造队列」为业务步骤）

- `toXxx`：`EnsureMaterialized` → 委托 `_inner.toXxx`  
- `queryXxx` / `doXxx` / `count` / `exist`：Flush 后委托 `_inner`  
- 扩展实体 `insert` / `update` / `find*` 等：内部驱动构造 API 后走执行出口

### 5.3 特殊方法

| 方法 | 约定 |
|------|------|
| `clear` / `reset` / `clearSelect` / `clearWhere` / `clearPage` | 清空门面 `_steps` **并** `_inner.clear()`（推荐 **立即生效**，避免脏读） |
| `copy()` | 复制 DB/路由配置 + **队列深拷贝**（今日不复制 SQL 状态；若兼容旧语义则文档标明「空 clone」vs「含脚本 clone」——建议新增 `copyScript()` 或明确 `copy` 含未物化队列） |
| `getBrotherBuilder()` | 返回新**门面**（或文档化返回内核）；嵌套闭包优先 `CreateChildFacade()`，共享 `ps`/seed 规则与 `StepBuilder.getBrotherBuilder` 对齐 |
| `record` / `stop` / `toApart` / `useApart` | 元编排；建立在门面 `_steps`（`IStep` 队列）切片上 |
| `configClear` / `print` / `setCache` / `setSeed` / `setPosition` / `setDBInstance` / route | **配置态**：可立即透传到 `_inner`（非 SQL 片段），不必进 `IStep` 队列 |
| `beginTransaction` / `useTransaction` / `commit` | 执行域：透传/委托 `_inner`，与 `IStep` 队列无关 |
| `addPara` | 建议立即写入 `ps`（副作用可见），或入队 `AddPara` 且 Flush 保序——须与参数命名策略统一 |

### 5.4 `ifs` / `opened` 门控

今日：`ifs(false)` 使下一次 `set`/`where*` 跳过。

延迟模型二选一（实现期锁定一种）：

1. **编排期求值（推荐）**：`ifs` 立即改变门控标志；后续 API 若被跳过则 **不入队**。语义与今日一致，实现简单。  
2. **入队期求值**：`Ifs` 步骤 + 条件常量入队，Flush 时跳过——仅当需要「脚本可编辑后再求值」时有价值，本期无必要。

### 5.5 嵌套 `Action<SQLBuilder>`

今日问题：`select(as, Action)` / `from(as, Action)` / `join(..., Action)` / `where(..., Action)` / CTE 等在闭包内 **立刻 `toSelect()`**，把方言 SQL 字符串嵌进父片段。

目标行为：

```
父.select("x", child => child.select("a").from("t"))
  → 父队列: SelectSubquery("x", childScript=[Select("a"), From("t")])
  → 父 Flush: 物化子脚本 → 子 toSelect → 拼进父 selectPart
```

要点：

- 闭包执行时机：**编排期执行闭包**，但闭包内 API 只写入 **子门面 `_steps`**，子 Builder 默认不提前 Dialect 渲染。  
- 子 SQL 字符串生成推迟到 **父 Flush**（或父 `toSelect`），以保持参数分配顺序与今日一致。  
- `getBrotherBuilder()` 仍共享 `ps`；paramKey 分配发生在 Flush，须用回归测试锁住键序（参考 `SQLBuilderApartTests` 参数键用例）。

### 5.6 UNION / CTE / Merge / Recur

扁平「方法名字符串列表」不足以表达图结构，步骤载荷需支持：

- `UnionBranchStep(IReadOnlyList<IStep> branch, bool all, …)`  
- `CteSelectStep(name, IReadOnlyList<IStep> inner)` / `CteSolidStep`  
- `Merge*` 步骤或保留 `MergeIntoBuilder` 为子编排器（其 `toMergeInto` 触发父/子 Flush）  
- `RecurCTEBuilder`：闭包录制为子脚本

---

## 6. 与现有代码的映射（承接 §4）

### 6.1 符号对照

| 改造前 | 改造后 |
|--------|--------|
| `SQLBuilder`（唯一类型） | `StepBuilder`（内核，逻辑冻结）+ `SQLBuilder`（新门面） |
| 调用即改 `SqlGoup` | 门面 `Enqueue(IStep)`；Flush 时 `step.Apply(_inner)` |
| `ApartEmitter` 事后快照 | 门面 `_steps` 直接导出 |
| `IApartStep.Apply(SQLBuilder)` | 演进为 `IStep.Apply(StepBuilder)` |
| `useSQL()` | 仍返回对外 `SQLBuilder`（新门面），内建 `StepBuilder` |

### 6.2 扩展方法与 SQLClip

- 扩展方法继续挂在 **新** `SQLBuilder` 上。  
- 若扩展曾读取半成品 `current`，经门面透传属性或先 `EnsureMaterialized()`。  
- SQLClip / Clause 持有门面引用；出口走门面 `to*` / `query*`。

### 6.3 Apart 迁移

| 阶段 | 行为 |
|------|------|
| P3–P4 | `toApart` 深拷贝门面 `_steps`；`useApart` 追加到 `_steps`（不立刻 Apply） |
| 过渡 | 未覆盖路径可 Flush 后 `ApartEmitter.Emit(_inner)` 兜底 |
| 完成 | 合并 `IApartStep` → `IStep`；删除事后 Emit 主路径 |

### 6.4 防重入（硬约束）

```
IStep.Apply        → 仅调用 StepBuilder.*
SQLBuilder.Enqueue → 仅门面 public 编排方法
useApart           → Enqueue 已有 IStep，不 Apply
EnsureMaterialized → 只 Apply，不再 Enqueue
```

### 6.5 语义对比测试（替代 DualWrite）

不在同一类型内双写。对比方式：

```text
链 A：new SQLBuilder()…toSelect()     // 门面 + 队列 + Flush
链 B：new StepBuilder()…toSelect()    // 旧实现直调
断言：sql 文本 + 参数键序一致
```

---

## 7. 参数与副作用

副作用发生在 **`IStep.Apply` → `StepBuilder` 方法体** 内（与今日一致），门面入队阶段不分配 `paramKey`：

1. **参数名生成**（`paraSeed` / `level` / 自增）——Flush 回放时按步骤顺序发生  
2. **`whereFormat` / `selectFormat` 占位替换**——同上  
3. **Auth / AOP 钩子**——仍在 `StepBuilder` 原调用点触发（即 Flush 期）  
4. **自动清理** `_AutoClearWay`：执行后清空门面 `_steps` + `_inner.clear()`，与今日对齐  

嵌套场景：父步骤 `Apply` 内按旧控制流创建 brother `StepBuilder` 并回放子 `IStep`，以保持参数写入顺序。

---

## 8. 风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| 更名漏改 | 编译失败或类型错位 | P1 单独提交；全量编译 + 测试指到 StepBuilder/门面 |
| 门面签名遗漏 | 二进制/源码不兼容 | P0 API 清单与 P2 桩生成对照 |
| Step 类爆炸 | 文件多、难导航 | 按家族分目录；可脚本生成空 Step |
| `Apply` 误调门面 | 死循环 / 双倍入队 | Code review；`Apply` 参数类型固定为 `StepBuilder` |
| 嵌套参数序变化 | SQL/键不一致 | 门面 vs StepBuilder 对比测试 |
| 中间态未 Flush | Count 为 0 | getter 内 `EnsureMaterialized` |
| `ifs` 误录 | 多/少条件 | 编排期门控，跳过则不 `Enqueue`（§5.4） |
| 重复 Flush 性能 | 多余 clear+重放 | `_dirty` 标志；无变更不重放 |
| 文档漂移 | Skills/教程仍写旧结构 | P5 同步 |

---

## 9. 任务拆解（可立项）

### T0 — API 清单与基线（0.5–1d）

- [ ] 导出原 `SQLBuilder` 全部 public 方法/属性/索引器清单（含重载）  
- [ ] 标注每项：`Enqueue` / `Flush+委托` / `透传配置` / `元操作立即生效`  
- [ ] 固化回归：`SQLBuilderTests` / `Apart` / `Exist` / `SubqueryTop` / `Extension` / `Route`

### T1 — 更名为 StepBuilder（1d）

- [ ] `partial class SQLBuilder` → `StepBuilder`（全部分片 + 建议文件名同步）  
- [ ] 内部 `new SQLBuilder` / 返回类型自引用全部改为 `StepBuilder`  
- [ ] **方法体零逻辑改动**；编译通过  
- [ ] 过渡期测试可直连 `StepBuilder` 验证旧行为仍绿

### T2 — 新 SQLBuilder 门面骨架（1–2d）

- [ ] 新建 `SQLBuilder`：`List<IStep> _steps` + `StepBuilder _inner`  
- [ ] 定义 `IStep`（`void Apply(StepBuilder builder)`）  
- [ ] 按清单生成 **全部 public 空方法/属性**（同签名）  
- [ ] 属性默认透传 `_inner`；执行类方法可先写 `EnsureMaterialized()+委托`  
- [ ] `useSQL()` / 工厂返回新门面

### T3 — 一方法一 Step：无嵌套家族（3–5d）

- [ ] 为 `select(string)` / `from` / `orderBy` / `groupBy` / `setPage` / `skipTake` / `distinct` / `top` 等建立 Step 类  
- [ ] 简单 `where*` / `set*` / `setTable` 入队  
- [ ] 门面 vs `StepBuilder` 对比断言转绿

### T4 — Where 全家桶与控制流（3–5d）

- [ ] 每个 where 重载一个 Step 类；`sink`/`rise`/`pin`/`whereIn`…  
- [ ] `ifs` 编排期门控  
- [ ] `clear`/`reset` 清队列 + `_inner`

### T5 — 嵌套 / UNION / CTE / Merge（3–5d）

- [ ] `Action<SQLBuilder>`：子门面录制 → 父 Step 持有子步骤列表  
- [ ] UNION / CTE / Recur / MergeInto 边界  
- [ ] Apart：`toApart`/`useApart` 基于 `_steps`

### T6 — 收尾（1–2d）

- [ ] `StepBuilder` 改为 `internal`（若可行）  
- [ ] 移除 ApartEmitter 主路径；更新 API 说明与 Skills  
- [ ] 全量回归 + 性能抽检

**合计量级（粗估）**：约 2–3 人周；Step 类可用清单脚本批量生成空壳以降低手工量。

---

## 10. 验收用例（设计级）

| # | 场景 | 期望 |
|---|------|------|
| A1 | 链式 `select/from/where` 后、`toSelect` 前 | `_steps` 非空；`_inner` 片段仍空（或仅初始态） |
| A2 | 同链：门面 `toSelect` vs 纯 `StepBuilder` | SQL + 参数键一致 |
| A3 | `select(as, Action)` 子查询 | 与 StepBuilder 直调一致 |
| A4 | `record`→`stop`→`useApart` | 与手动链等价 |
| A5 | `ifs(false).where(...)` | 不入队 / 无该条件 |
| A6 | UNION / CTE / Merge | 结构正确 |
| A7 | `doUpdate` 空 where 保护；`clear` 后队列空 | 与现网一致 |
| A8 | `queryPaged` / `exist` / `count` | Flush 后委托 `_inner` |
| A9 | 每个清单内 public 构造方法均有对应 `*Step` 类型 | 一方法一类型审计通过 |

---

## 11. 目录与文档约定

```
pure/src/ado/builder/
  SQLBuilder.cs              # 新门面（可 partial：SQLBuilder.queue.cs 等）
  SQLBuilder.*.cs            # 门面 API 分片（select/where/save/dymatic…）
  steps/
    IStep.cs
    select/SelectStep.cs
    where/WhereKeyValStep.cs
    ...                      # 一方法一文件（或一族一文件，类仍一对一）
  StepBuilder.cs             # 原 SQLBuilder 更名
  StepBuilderWhere.cs        # 原分片更名
  StepBuilderSelect.cs
  StepBuilderDymatic.cs
  StepBuilderSave.cs
  StepBuilder.apart.cs       # 内核侧 Apart 逻辑；对外 API 可挂在门面
  apart/                     # 演进为 IStep 序列化
  SQLKit/                    # 仍只被 StepBuilder 使用
```

本文档路径：`doc/design/features/SQLBuilder-延迟构造重构.md`。  
实施决策变更回写 §4 / §5 / §12。

---

## 12. 决策记录

| ID | 议题 | 建议默认 | 状态 |
|----|------|----------|------|
| D0 | 实施方式 | **更名 StepBuilder + 新 SQLBuilder 门面 + IStep 队列**；原方法体不动 | **已锁定** |
| D7 | 门面形态（过渡） | 门面 **继承** `StepBuilder`（非组合），用 `new` 隐藏已接入 API；`IStep.Apply(StepBuilder)` 走基类不重入 | **实施中** |
| D8 | 默认入队策略 | 默认 **纯延迟**（仅入队，出口 Flush）；`useDeferred(false)` 可临时双写对照 | **已切换** |
| D9 | Step 生成 | `tools/gen_sqlbuilder_steps.py` 生成简单 + `Action<>` 构造 API；手写核心保留在 `SQLBuilder.defer.cs` | **实施中** |
| D1 | `ifs` 编排期求值 vs 入队求值 | 编排期求值；跳过则不 Enqueue | 待确认 |
| D2 | `copy()` 是否复制未物化队列 | 复制 `_steps` + 配置；`_inner` 干净或随 Flush | 待确认 |
| D3 | Auth/`fireBuild*` 触发点 | 保持在 StepBuilder 原路径（Flush 期） | 待确认 |
| D4 | StepBuilder 可见性 | 最终 `internal`；迁移期可 `public` | 待确认 |
| D5 | Apart `record`/`stop` | 门面队列切片语法糖；构造 API 始终入队 | 待确认 |
| D6 | 空方法阶段是否允许执行方法先委托 | 允许（P2 即 Flush+委托） | 待确认 |

---

## 附录 A — 现状关键入口速查

| 符号 | 改造后位置（预期） |
|------|-------------------|
| 门面编排 | 新建 `SQLBuilder*.cs` + `steps/*` |
| 内核构造/执行 | `StepBuilder*.cs`（原 `SQLBuilder*.cs`） |
| `toSelect` 内核 | `StepBuilder.toSelect`；门面先 Flush 再委托 |
| 嵌套提前 `toSelect` | 仍在 `StepBuilderSelect`；由对应 `*SubqueryStep.Apply` 触发 |
| Apart | 门面导出 `_steps`；过渡期 `apart/ApartEmitter` |
| Apart 测试 | `Tests/TestBug/src/TestPure/SQLBuilderApartTests.cs` |

## 附录 B — 一句话结论

> **原 SQLBuilder 整体更名为 StepBuilder 且方法体不动；新建同名 SQLBuilder 作编排门面，内建 `List<IStep>`，一 public 构造方法一 Step 类；执行前 Flush 回放到 StepBuilder，再走既有 to/query/do。**
