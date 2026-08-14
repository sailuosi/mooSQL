# SQLBuilder 抽象基类与 PrepareSQLBuilder 重命名

> 面向 **mooSQL 项目开发人员**。一次**看起来像大重构、实际以类型层级重定位为主**的改造。  
> 承接 [延迟构造重构](./SQLBuilder-延迟构造重构.md)、[执行模板缓存](./SQLBuilder-执行模板缓存.md)。  
> 用户侧链式 API 签名与语义不变；本文不替代 [SQLBuilder 使用文档](../../docs/SQL/basis/SQLBuilder.md)。

---

## 1. 一句话结论

把 **`SQLBuilder` 升格为抽象类（公共 API 宿主）**；**`StepBuilder` 改为继承它**（无编排缓存的 eager 实现）；**现有编排门面 `SQLBuilder` 更名为 `PrepareSQLBuilder`**（延迟构造 + 模板缓存实现）。对外仍以 `SQLBuilder` 类型编程，默认工厂继续产出 Prepare 实现。**各类子查询 / 嵌套构建的声明一律为 `SQLBuilder`**（不得再写 `StepBuilder` / `PrepareSQLBuilder`）。

> **实施注记（已落地）**：默认实现以 `abstract partial class SQLBuilder` 承载原门面方法体（`virtual`），`PrepareSQLBuilder` 为可 `new` 的薄子类；`StepBuilder` 经 `KernelCtorMarker` 内核构造并 `override` 为 eager。`useSQL()` → `new PrepareSQLBuilder()`。

---

## 2. 背景与动机

### 2.1 现状（改造前）

```text
SQLBuilder（具体类，编排门面）
  └─ 组合 _inner: StepBuilder（eager 内核）

useSQL() → new SQLBuilder() → Flush / 模板缓存 → StepBuilder
```

| 类型 | 角色 |
|------|------|
| `SQLBuilder` | 对外门面：入队 `IStep`、`runBuild`、ScriptTemplate 热路径；属性/事务多经 `proxy` 转发 `_inner` |
| `StepBuilder` | 原实现更名而来：立即改 `SqlGoup` / where，真正拼装与执行 |

问题不在行为对错，而在**类型语义**：

| 问题 | 表现 |
|------|------|
| **名称倒置** | 用户口中的「SQLBuilder」应是「SQL 构建器」这一概念；实际具体类却是带队列/缓存的 Prepare 门面 |
| **Step 与门面平行** | `StepBuilder` 与 `SQLBuilder` 无继承关系，扩展方法、泛型约束、`Action<SQLBuilder>` 无法直接覆盖内核直连场景 |
| **双份 API 表面** | 门面 partial（`defer.api` / `proxy`）与内核 partial（`StepBuilder*`）各挂一套同名方法，继承缺失时只能靠转发维持返回类型 |

### 2.2 设计目标

| 目标 | 说明 |
|------|------|
| **SQLBuilder = 抽象公共面** | 所有对外 public 方法/属性声明（及可共享的默认行为）落在抽象类上 |
| **StepBuilder = 无缓存实现** | `StepBuilder : SQLBuilder`；调用即构造；**不**走 `IStep` 队列 / ScriptTemplate 编排缓存 |
| **PrepareSQLBuilder = 现门面** | 现有 `SQLBuilder` 整体更名；保留延迟构造、编排 Hash、模板缓存 |
| **对外兼容** | 业务代码继续写 `SQLBuilder`、`useSQL()`、`Action<SQLBuilder>`；默认仍是 Prepare 实现 |
| **子查询声明统一** | 凡子查询 / 嵌套构建相关的参数、返回、委托，**一律声明为 `SQLBuilder`**，禁止再出现 `Action<StepBuilder>` 等具体实现类型（见 §5.4） |
| **变动面可控** | 以更名 + 继承接线为主；**尽量不改** Step 方法体与 Prepare 入队/Flush 逻辑 |

### 2.3 非目标（本期不做）

- 不合并或删除 `IStep` / `runBuild` / ScriptTemplate（Prepare 路径保持）  
- 不把 StepBuilder 的 eager 逻辑「搬回」抽象基类实现细节里大改  
- 不强制去掉 Prepare 对内核的组合（`_inner`）；继承解决的是**类型身份**，不是必须改成「纯虚方法全量 override」  
- 不改变 Dialect / Repository / SQLClip 对外契约（仅适配类型名与工厂返回）

### 2.4 成功判据

1. `SQLBuilder` 为 `abstract`；业务与扩展方法参数/返回类型仍指向它。  
2. `StepBuilder : SQLBuilder`，直连调用与改造前 eager 行为一致（无编排缓存）。  
3. 原门面类型名为 `PrepareSQLBuilder : SQLBuilder`；`useSQL()` 默认 `new PrepareSQLBuilder(...)`。  
4. **各类子查询**相关签名中不再出现 `StepBuilder` / `PrepareSQLBuilder`，一律为 `SQLBuilder`。  
5. 既有回归（快照、Apart、Exist、模板缓存冷热路径、withRecurTo 等）全绿。  
6. 性能对照测试可表述为：`PrepareSQLBuilder`（关/开模板缓存）vs `StepBuilder`（基线），而非「两个无关类型」。

---

## 3. 目标类型关系

### 3.1 改造后

```text
                    SQLBuilder（abstract）
                   /                    \
        StepBuilder                 PrepareSQLBuilder
     （eager / 无编排缓存）         （原 SQLBuilder 门面）
                                         │
                                         └─ 组合 _inner: StepBuilder
                                            （Flush / 冷路径宿主，可不变）
```

### 3.2 术语对照

| 术语 | 含义 |
|------|------|
| **SQLBuilder** | 抽象公共 API；链式返回 `SQLBuilder`；用户编程入口类型 |
| **StepBuilder** | 无缓存实现：构造即写状态；可作性能测试基线、特殊直连场景 |
| **PrepareSQLBuilder** | 有准备过程的实现：入队 →（可选模板命中）→ Flush → 内核执行 |
| **默认实现** | 工厂 / `useSQL()` 返回的具体类型 = `PrepareSQLBuilder` |
| **子查询声明** | select/from/join/where/CTE/union/merge 等嵌套闭包与对外返回的 Builder 类型 = **`SQLBuilder`**（§5.4） |
| **无缓存** | 相对「编排 ScriptTemplate / OrchestrationHash 热路径」而言；与结果缓存 `setCache` 等产品能力无关 |

### 3.3 与旧文档用语的映射

| [延迟构造重构](./SQLBuilder-延迟构造重构.md) 旧称 | 本文新称 |
|--------------------------------------------------|----------|
| SQLBuilder（新门面） | **PrepareSQLBuilder** |
| StepBuilder（原实现） | **StepBuilder**（不变），但改为 **继承** 抽象 `SQLBuilder` |
| （无对应） | **SQLBuilder** = 抽象基类 |

后续设计/注释若仍写「门面 SQLBuilder」，应读作 **PrepareSQLBuilder**；若写「对外类型 SQLBuilder」，读作 **抽象基类**。

---

## 4. 为何「看起来大、实际小」

| 看起来很大 | 实际主要是 |
|------------|------------|
| 全库 `SQLBuilder` 符号海量出现 | 多数引用**继续合法**：指向抽象基类即可 |
| 两套 Builder 类图重画 | 一次更名 + ` : SQLBuilder` + 工厂改 `new` |
| API 要「抽到基类」 | 声明上收；实现可仍在原 partial 文件，或基类声明 + 子类已有实现 |
| 返回类型统一 | 链式方法返回 `SQLBuilder`（本来门面已是）；Step 侧 `return this` 协变到基类 |

**刻意不做的大活：**

- 不重写 where/select 算法  
- 不重做 Apart / Step 代码生成主路径  
- 不要求 Prepare 改为「去掉 `_inner`、全部 virtual 下推」——组合可保留，降低风险

---

## 5. 职责划分（锁定）

### 5.1 抽象类 `SQLBuilder`

| 纳入 | 说明 |
|------|------|
| 全部对外 **public** 方法签名 | `select` / `where` / `toXxx` / `queryXxx` / `doXxx` / 事务 / 配置等 |
| 对外 **public** 属性签名 | `DBLive` / `Dialect` / `ps` / …（实现可 abstract 或由子类提供） |
| 链式返回类型 | 一律 `SQLBuilder`（或嵌套 DSL 类型仍返回门面抽象） |
| 可选：少量真正共享的非虚工具 | 仅当两处实现已完全一致且无状态分歧时再上收；**默认不上收实现** |

| 不强制纳入基类实现体 | 说明 |
|----------------------|------|
| `List<IStep>` / `runBuild` | 仅 Prepare |
| ScriptTemplate / OrchestrationHash | 仅 Prepare |
| `SqlGoup` 立即改写 | 仅 Step（及 Prepare 的 `_inner`） |

基类方法形态建议（实施时二选一，文档锁定偏好 **A**）：

| 方案 | 做法 | 取舍 |
|------|------|------|
| **A（偏好）** | 基类对「有两套实现」的 API 标 `abstract`；Prepare / Step 各自已有 partial 实现挂到 override | 改动清晰，编译器强制两边齐套 |
| B | 基类放 `virtual` 空/抛 `NotImplemented`，子类 override | 易漏 override，仅作过渡 |

嵌套委托统一规则见 **§5.4**（锁定）。

### 5.2 `StepBuilder : SQLBuilder`

- **行为**：与今日内核一致——调用即改 `SqlGoup` / where / CTE 等。  
- **缓存**：不维护编排步骤队列；不参与 ScriptTemplate 热路径（可作 perf 基线）。  
- **可见性**：可继续 `public`（测试直连）；若需收口可后续 `internal` + 工厂，**非本期硬性**。  
- **返回**：`public override SQLBuilder select(...) { …; return this; }`（或等价）。

### 5.3 `PrepareSQLBuilder : SQLBuilder`

- **来源**：当前具体类 `SQLBuilder` 整体更名（`SQLBuilder.cs` / `defer.*` / `proxy` / `cache` / `apart` 等 partial）。  
- **行为**：构造 API 入队；执行前 Flush（或模板命中）；属性/事务仍可转发 `_inner`。  
- **组合**：保留 `StepBuilder _inner` 作为物化宿主——**本期不强制改为「Prepare 自己当内核」**。  
- **注意**：`_inner` 已是 `SQLBuilder` 子类；Flush 时 `Apply` 目标类型仍写 `StepBuilder`（或接受 `SQLBuilder` 再断言），避免误对另一 Prepare 入队。

### 5.4 子查询 / 嵌套构建：声明一律 `SQLBuilder`（锁定）

> **规则**：凡「再开一个构建器去写一段 SQL」的 API——无论实现落在 Prepare 还是 Step——**对外与 partial 方法签名中的 Builder 类型一律写 `SQLBuilder`**。  
> 运行时具体实例可以是 `PrepareSQLBuilder` 或 `StepBuilder`；**声明层不得泄露具体类**。

#### 覆盖范围（非穷尽，按此模式扫全库）

| 类别 | 典型 API | 声明应为 |
|------|----------|----------|
| SELECT 列子查询 | `select(string asName, Action<…> doColSelect)` | `Action<SQLBuilder>` |
| FROM 子查询 | `from(string asName, Action<…> childFromPart)` | `Action<SQLBuilder>` |
| JOIN 子查询 | `join` / `leftJoin` / `innerJoin` / `rightJoin`（带 `Action` 重载） | `Action<SQLBuilder>` |
| WHERE 子查询 | `where(…, Action<…>)`、`whereIn` / `whereNotIn` / `whereExist` / `whereNotExist` | `Action<SQLBuilder>` |
| WHERE 分组闭包 | `where(Action<…>)`、`or(Action<…>)` | `Action<SQLBuilder>` |
| CTE | `withSelect` / `withAs` / `withRecur` | `Action<SQLBuilder>` 或等价嵌套 DSL 入参为 `SQLBuilder` |
| 递归 CTE 门面 | `RecurCTEBuilder.whereRoot` / `whereNext` | `Action<SQLBuilder, RecurCTEBuilder>` |
| UNION | `union(Action<…>)` | `Action<SQLBuilder>` |
| MERGE | `mergeUsing(…, Action<…> buildSelect)` | `Action<SQLBuilder>` |
| 兄弟 / 工厂类返回 | `getBrotherBuilder` / `copy` / 子路径 `Attach` 的**对外**返回 | `SQLBuilder` |
| Step / IStep 回放 | `Apply` 若需把「子构建器」交给业务闭包 | 闭包参数 `SQLBuilder`；内核字段可仍持 `StepBuilder` |

#### 明确禁止

| 禁止 | 原因 |
|------|------|
| `Action<StepBuilder>` / `Func<StepBuilder, T>` 出现在 public / 扩展方法签名 | 调用方被钉死在 eager 实现，无法与 Prepare 共用 |
| `Action<PrepareSQLBuilder>` | 同上，钉死在编排实现 |
| 子查询重载一边 `SQLBuilder`、一边 `StepBuilder` 长期并存 | 双表面；本期应收到单一声明 |

#### 允许保留具体类型的地方（内部实现，非「子查询声明」）

| 允许 | 说明 |
|------|------|
| `PrepareSQLBuilder` 私有字段 `_inner` | 类型 `StepBuilder` |
| `IStep.Apply(StepBuilder host)` 或内部 Flush 宿主 | 物化目标；不暴露给业务闭包 |
| 测试里 `new StepBuilder()` / `BeOfType<PrepareSQLBuilder>()` | 测试基础设施 |

#### 改造动作（Step 侧）

现有 `StepBuilder*` 中若仍写 `Action<StepBuilder>` / 返回 `StepBuilder` 的子查询重载：

1. 签名改为 `Action<SQLBuilder>`，方法返回 `SQLBuilder`（`return this`）。  
2. 闭包调用处传入 `this`（已是 `SQLBuilder`）即可；若内部曾 `new StepBuilder()` 再交给用户，改为按父级同实现族创建（Step 路径新建 `StepBuilder`，Prepare 路径 `Attach` / 子 `PrepareSQLBuilder`），**但变量静态类型仍是 `SQLBuilder`**。  
3. 生成器（`gen_sqlbuilder_steps.py` 等）模板中的委托类型同步改为 `SQLBuilder`。

---

## 6. 工厂与入口

| 入口 | 改造后 |
|------|--------|
| `DBInstance.useSQL()` / 扩展 `useSQL()` | `return new PrepareSQLBuilder(...);`（静态类型 `SQLBuilder`） |
| `new SQLBuilder()`（业务） | **不可用**（抽象）；应改为 `useSQL()` 或 `new PrepareSQLBuilder()` |
| 测试直连内核 | `new StepBuilder()` 或现有辅助方法 |
| `SQLBuilder.Attach(StepBuilder, …)` | 迁到 `PrepareSQLBuilder.Attach`；若扩展仍暴露，返回类型为 `SQLBuilder` |

文档与示例中的 `new SQLBuilder()` 改为 `db.useSQL()` 或明示 `new PrepareSQLBuilder()`。

---

## 7. 文件与更名清单（建议）

### 7.1 类型更名

| 现类型 | 新类型 | 文件建议 |
|--------|--------|----------|
| `SQLBuilder`（门面具体类） | `PrepareSQLBuilder` | `SQLBuilder.*.cs` → `PrepareSQLBuilder.*.cs`（或暂留文件名、只改类型；**推荐文件名同步**） |
| （无） | `SQLBuilder` abstract | 新建 `SQLBuilder.cs`（或 `SQLBuilder.abstract.cs`）承载声明 |
| `StepBuilder` | `StepBuilder : SQLBuilder` | 现有 `StepBuilder*.cs`；类声明加基类 |

### 7.2 符号替换注意点

- 门面自引用：`new SQLBuilder(` → `new PrepareSQLBuilder(`  
- 注释/XML：`见 SQLBuilder.defer` → `PrepareSQLBuilder`  
- 测试类名可不改（`SQLBuilderXxxTests` 测的是对外行为）；断言类型处 `BeOfType<PrepareSQLBuilder>()` 按需  
- `tools/gen_sqlbuilder_steps.py` 等生成物：生成目标命名空间/宿主类型改为挂在抽象 API 或 Prepare，按生成器现约定改一处模板即可  
- **子查询委托**：全局检索 `Action<StepBuilder` / `Func<StepBuilder` → 改为 `SQLBuilder`（§5.4）  
- **不要**把业务代码里的 `SQLBuilder` 参数类型改成 `PrepareSQLBuilder`——应留在抽象层

### 7.3 扩展方法

`MooSQLBuilderExtensions` / `SQLBuilderExtensions` 继续扩展 **`this SQLBuilder`**。  
Prepare 与 Step 均可调用；内部若依赖编排队列，需 `is PrepareSQLBuilder` 或抽 `virtual` 钩子（仅当确实有分叉时再加，避免到处类型判断）。

---

## 8. 实施顺序（低风险）

```text
Phase 0  冻结行为：跑通现有 Pure 回归 + 快照，记基线
Phase 1  引入 abstract SQLBuilder（先空壳/仅部分签名），Prepare 暂仍叫 SQLBuilder 且 : 基类（过渡可编译）
Phase 2  StepBuilder : SQLBuilder；子查询/嵌套委托与返回类型一律改为 SQLBuilder（§5.4）；消除与基类重复的冲突成员
Phase 3  具体门面更名 SQLBuilder → PrepareSQLBuilder；工厂改 new；全局编译错误清零
Phase 4  上收/补齐基类 abstract 声明，删掉「两套无关 public 表面」的重复声明（若有）
Phase 5  文档、技能、perf 测试表述对齐；可选 Obsolete 引导 new PrepareSQLBuilder
         验收：库内 public 签名无 Action<StepBuilder> / Action<PrepareSQLBuilder>
```

**禁止**：在 Phase 1–3 顺手大改 Flush、Hash、模板缓存或 where 拼装。

---

## 9. 风险与边界

| 风险 | 缓解 |
|------|------|
| 抽象成员漏 override 导致编译失败面大 | 按 partial 模块分批上收签名；先虚后 abstract |
| `Attach` / 子查询闭包误 new 抽象类 | 工厂与 `Attach` 只产出具体子类；闭包参数静态类型仍为 `SQLBuilder` |
| Step 侧残留 `Action<StepBuilder>` | Phase 2 按 §5.4 一次性改完；生成器模板同步 |
| 扩展方法内假设「一定有 `_steps`」 | 编排能力留在 Prepare；或 `virtual`/`is` 显式分支 |
| 序列化 / 反射依赖具体类名 | 检索 `typeof(SQLBuilder)`、Activator、文档示例 |
| 二重身份：Prepare 的 `_inner` 也是 SQLBuilder | Flush/Apply **只**打到 Step；禁止对 Prepare 再套 Prepare 当 inner |

---

## 10. 对既有设计文档的影响

| 文档 | 读法更新 |
|------|----------|
| [延迟构造重构](./SQLBuilder-延迟构造重构.md) | 「新 SQLBuilder」→ PrepareSQLBuilder；并补充「抽象 SQLBuilder 为公共面」 |
| [执行模板缓存](./SQLBuilder-执行模板缓存.md) | 缓存宿主 = PrepareSQLBuilder |
| [Step 标记与编排 Hash](./SQLBuilder-Step标记与编排Hash.md) | 编排磁带属于 Prepare |
| [延迟参数解析](./SQLBuilder-延迟参数解析.md) | 同上 |
| [withRecurTo 门面衔接](./SQLBuilder-withRecurTo门面衔接.md) | 「门面」= Prepare；闭包参数类型仍是抽象 SQLBuilder |

不必立刻重写全文；在各文首增加一句指向本文即可（实施 PR 时可顺手改）。

---

## 11. 验收清单

- [ ] `SQLBuilder` 为 abstract；无法 `new SQLBuilder()`  
- [ ] `PrepareSQLBuilder` / `StepBuilder` 均 `: SQLBuilder`  
- [ ] `useSQL()` 返回 Prepare，静态类型为 `SQLBuilder`  
- [ ] **子查询/嵌套**：public 与扩展方法中无 `Action<StepBuilder>` / `Action<PrepareSQLBuilder>`；一律 `Action<SQLBuilder>`（§5.4）  
- [ ] `getBrotherBuilder` / `copy` / CTE·UNION·MERGE 闭包入参与对外返回均为 `SQLBuilder`  
- [ ] Step 直连与 Prepare（关模板缓存）SQL 语义一致（已有对照测试可改名描述）  
- [ ] 模板缓存冷/热路径仍仅作用于 Prepare  
- [ ] Apart / withRecurTo / Exist / 快照回归通过  
- [ ] 技能与 `doc/docs` 中「门面 vs 内核」表述与本文一致  

---

## 12. 附录：最小伪代码

```csharp
namespace mooSQL.data
{
    public abstract partial class SQLBuilder : IDisposable
    {
        public abstract SQLBuilder select(string columns);
        public abstract SQLBuilder select(string asName, Action<SQLBuilder> doColSelect);
        public abstract SQLBuilder from(string fromPart);
        public abstract SQLBuilder from(string asName, Action<SQLBuilder> childFromPart);
        public abstract SQLBuilder where(string key, object val);
        public abstract SQLBuilder where(string key, Action<SQLBuilder> doselect);
        public abstract SQLBuilder withSelect(string name, Action<SQLBuilder> doselect);
        public abstract SQLBuilder union(Action<SQLBuilder> doUnion);
        // … 其余公共 API：凡嵌套构建器均为 SQLBuilder …

        public abstract SQLCmd toSelect();
        public abstract DataTable query();
        // …
    }

    public partial class StepBuilder : SQLBuilder
    {
        public override SQLBuilder select(string columns)
        {
            // 既有 eager 逻辑不变
            return this;
        }
    }

    public partial class PrepareSQLBuilder : SQLBuilder
    {
        private readonly StepBuilder _inner;
        private readonly List<IStep> _steps;

        public override SQLBuilder select(string columns)
        {
            // 既有入队逻辑不变
            return this;
        }

        public override DataTable query()
        {
            // 既有 Flush / 模板缓存 / 委托 _inner 不变
            …
        }
    }
}
```

工厂：

```csharp
public SQLBuilder useSQL() => new PrepareSQLBuilder().setDBInstance(this);
```
