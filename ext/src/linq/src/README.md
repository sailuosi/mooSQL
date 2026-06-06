# mooSQL.Ext LINQ 子项目

## 架构定位

**Ext LINQ 对标 EF Core、Linq2DB、SqlSugar `Queryable` 等框架的通用 LINQ 能力**，入口采用与业界一致的 **标准 Queryable 习惯**：

- `db.useQueryable<T>()` / `db.AsQueryable<T>()` → `ITable<T>`（`GetTable<T>()` 为 Linq2DB 兼容）
- 标准 `System.Linq.Queryable` 链式（Where / Select / OrderBy / GroupBy …）
- Linq2DB 风格扩展（LoadWith、Merge、SetOp、InsertOrUpdate 等）

**与 Fast LINQ 的关系**：Fast（`pure/src/linq`）经 **`useBus`** 走 mooSQL 特色 `IDbBus` + `BusQueryable`，两条主线 **并行、不互相替代**。详见 [LINQ全景分析与项目对比.md](../LINQ全景分析与项目对比.md)。

## 项目说明

本子项目是对 **mooSQL.Pure** 核心库的增强层，主要提供：

- 基于实体类的 **LINQ 表达式**查询（`IQueryable<T>`）
- 表达式树 → SQL 语句结构的编译
- 编译结果通过 Pure 层 `SQLBuilder` / `ClauseTranslateVisitor` 执行
- 实体映射、导航属性（`LoadWith`）、方言扩展等衍生能力

**边界原则：** 本子项目 **不包含** 数据库连接、方言实现、SQL 执行器等与 Pure 重合的能力；执行一律委托给 `DBInstance` + Pure 层基础设施。

---

## 设计目标

### 与 Fast LINQ 的分工

| | **Fast LINQ**（Pure） | **Ext LINQ**（本目录） |
|--|----------------------|------------------------|
| 入口 | `useBus` / `useDbBus` | `useQueryable` / `AsQueryable` |
| 目标 | mooSQL 特色、项目实践 | 对标 EF / 通用 Queryable |
| 编译 | 单阶段 → SQLBuilder | Compile → SentenceBag → Execute |

### 问题背景

早期 `ExpressionBuilder`（现 **`ClauseSqlTranslator`**）是一个「巨型类」，同时承担：

1. 表达式树 → `SelectQueryClause`（Statement）编译
2. 投影表达式 → `DbDataReader` 行映射（`BuildMapper` / `SetRunQuery`）
3. 查询执行与前置查询（Preamble / EagerLoad）

职责耦合导致：编译逻辑难以测试、执行路径难以替换、与 FastLinq 的 `query<T>()` 映射方式无法对齐。

### 核心思路

**编译与执行彻底分离**，形成三层流水线：

```
Expression（LINQ 表达式树）
    ↓  Compile
SentenceBag（Statement + 元数据）
    ↓  Execute
SQLBuilder → query<T>() → 实体结果
```

- **Compile 阶段**：只产出「可翻译的 SQL 语句结构」（`BaseSentence` / `SelectQueryClause`），以及导航列、参数等元数据。
- **Execute 阶段**：Statement 经优化 → `ClauseTranslateVisitor` → `SQLBuilder`，实体映射完全交给 Pure 的 `query<T>()`，不再生成 `DbDataReader` Mapper 表达式。

这与 FastLinq 的设计哲学一致：**表达式负责描述查询，SQLBuilder 负责物化结果**。

---

## 三层架构

```
┌─────────────────────────────────────────────────────────────────┐
│  用户 API                                                        │
│  ExpressionQuery / EntityProvider / IQueryable 扩展             │
└───────────────────────────┬─────────────────────────────────────┘
                            │ Expression
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│  Layer 1 — Compile（编译）                                       │
│  StatementCompileSession.VisitRoot（ClauseCompiler 根入口）        │
│    → ClauseExpressionVisitor + ClauseMethodVisitor（Buddy）       │
│    → StatementCall → StatementExpression（树上产物）              │
│    → IBuildContext / ISequenceBuilder（工具，非编排器）           │
│    → ClauseCompiler.Build → SentenceBag                          │
└───────────────────────────┬─────────────────────────────────────┘
                            │ SentenceBag（Statement）
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│  Layer 2 — Statement（语句包）                                   │
│  SentenceBag / SentenceItem                                      │
│  - Sentences[]: BaseStatement                                    │
│  - NavColumns: LoadWith 导航列                                   │
│  - srcExp / EntityType / Parameterized                           │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│  Layer 3 — Execute（执行）                                       │
│  BasicSentenceRunner → SentenceExecutor                            │
│    → SqlOptimizer.Finalize                                       │
│    → ClauseTranslateVisitor → SQLBuilderClause                   │
│    → SQLBuilder.query<T>()                                       │
│    → NavColumnLoader（导航属性二次查询）                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 编译阶段（Layer 1）

### 入口

| 入口 | 说明 |
|------|------|
| `ClauseCompiler.Build<T>()` | 内部编译，产出 `SentenceBag<T>` |
| `QueryMate.GetQuery<T>()` | 带缓存的对外入口 |
| `EntityVisitCompiler` / `EntityQueryCompiler` | 预编译委托，直接调用 `SentenceExecutor` |

### 双访问器模型

编译 MethodCall 链时使用 **Buddy 双访问器**（对齐 FastLinq），`ClauseSqlTranslator` 仅保留 SQL 语义能力：

```
MethodCallExpression
    → CallUntil.CreateCall(node)        // 强类型 MethodCall 节点
    → ClauseMethodVisitor.VisitXxx      // 按方法名分发
    → ISequenceBuilder / IBuildContext  // 复用既有 Builder 业务逻辑
```

| 组件 | 职责 |
|------|------|
| `ClauseExpressionVisitor` | 按 `ExpressionType` 分发序列根（`VisitConstant`/`VisitMember`/`VisitLambda`/`VisitExtension` 等）；已注册 MethodCall → `ClauseMethodVisitor`；未注册 Call → 扩展 Builder |
| `ClauseMethodVisitor` | 按 LINQ 方法名 VisitXxx，内联或 ApplyBuilder 到既有 Builder（对齐 FastLinq 双访问器） |
| `ClauseCompileContext` | 编译上下文（`StatementResult` 为唯一成功槽；`ToSentenceBag(stmt)` 组 Bag） |
| `StatementCompileSession` | 双访问器装配 + 统一 `VisitRoot`（根编译入口） |
| `StatementExpression` / `StatementCall` | 编译成功节点 / MethodVisitor 回传载体（对齐 Fast `ExpressionCall`） |
| `ClauseSqlTranslator` | SQL 语义引擎（MakeExpression / ConvertToSql / BuildWhere 等） |
| `ClausePredicateVisitor` | Where 谓词（Like/LikeLeft 统一 `Like` IR + 参数 substitute；逐步替代 Predicate.cs 部分逻辑） |
| `ClauseCompiler` | 编译收尾：收集 Statement、参数、NavColumns → `SentenceBag` |

### ISequenceBuilder 内聚度（Phase 5 评估）

- **保留独立类**：Merge / MultiInsert / 子查询嵌套等复杂算子（`MergeBuilder`、`MultiInsertBuilder` 等）。
- **已内联到 `ClauseMethodVisitor`**：Where / Select / OrderBy / **ThenBy** / Take-Skip / Join / Distinct / Contains / AllAny 等，经 `ToStatementCallOr` 回传 `StatementExpression`；子序列统一 **`ResolveSourceContext`**。
- **结论**：`ISequenceBuilder` 作为 **ClauseSqlTranslator 的工具** 保留；不再由单一 `ExpressionBuilder` 编排。

### MethodCall 分发策略

```
MethodCall
  ├─ 已内联 VisitXxx（高频、逻辑稳定）
  │     Where / Having / Select / OrderBy* / Take / Skip / Join*
  ├─ ApplyBuilder<T>（显式绑定 ISequenceBuilder，~45 个方法，见 Bindings.cs）
  │     见 ClauseMethodVisitor.Bindings.cs（由 gen_bindings.py 生成）
  ├─ DispatchPassThrough（AsQueryable / Alias 等透传）
  ├─ MooExt（DoUpdate / InjectSQL / SetPage / Sink 等，对标 FastMethodVisitor）
  ├─ Async（*AsyncCall + VisitXxxAsync）
  └─ 未注册 Call → ClauseExpressionVisitor.VisitMethodCall（扩展 Builder）
```

**内联文件：**

```
translator/
  ClauseMethodVisitor.Where.cs
  ClauseMethodVisitor.Select.cs
  ClauseMethodVisitor.OrderBy.cs
  ClauseMethodVisitor.TakeSkip.cs
  ClauseMethodVisitor.Join.cs
  ClauseMethodVisitor.Bindings.cs   # 自动生成
```

重新生成 Bindings：

```bash
python ext/src/linq/translator/tools/gen_bindings.py
```

### IBuildContext 与 Builder 体系（保留）

编译阶段的 **业务逻辑未重写**，仍由原有 Builder 体系完成：

- `ISequenceBuilder` / `MethodCallBuilder`：处理具体 LINQ 方法
- `IBuildContext`：维护 `SelectQueryClause`、投影、`MakeExpression`
- `ClauseSqlTranslator.MakeExpression` / `ConvertToSql`：表达式 → SQL 片段

根编译（`ClauseCompiler.Compile`）流程：

```
StatementCompileSession.Create(translator, buildInfo)
  → ExpandToRoot(expression)
  → VisitRoot(expression)  // 始终经 ClauseExpressionVisitor
  → StatementExpression → ClauseCompileContext.ToSentenceBag
```

`TryBuildSequence`（`[Obsolete]`，嵌套序列专用）：`ResolveSourceContext` 回退、各 `*Builder` 内部子序列解析。

### 已移除的编译期职责

| 已删除 | 原因 |
|--------|------|
| `ExpressionBuilder.BuildQuery<T>()` | 第二编译阶段（投影 + Mapper 绑定）不再需要 |
| `BuildMapper` / `ToReadExpression` | 实体映射改由 `query<T>()` 完成 |
| `FinalizeProjection` | 同上 |
| `SetRunQuery<T>()` | 不再在 Context 上绑定执行委托 |
| `ExpressionBuilder.EagerLoad.cs` | Preamble 前置查询链已废弃 |
| Preamble / `InitPreambles` | 执行阶段不再依赖前置查询数组 |

---

## 语句包（Layer 2）

### SentenceBag

`SentenceBag` / `SentenceBag<T>` 是编译与执行之间的 **唯一交接物**：

```csharp
SentenceBag
├── Sentences: List<SentenceItem>     // 待执行语句（通常 1 条 SELECT）
├── srcExp: Expression                // 原始 LINQ 表达式
├── EntityType: Type                  // 结果元素类型
├── NavColumns: Dict<Type, List<EntityColumn>>  // LoadWith 导航列
├── buildContext: IBuildContext       // 编译上下文（调试 / 二次投影）
├── IsFinalized: bool                 // 优化是否已完成
└── Runner: ISentenceRunner           // 执行策略（可覆盖）
```

`SentenceItem` 包含：

- `Statement: BaseSentence`（多为 `SelectSentence` → `SelectQueryClause`）
- `ParameterAccessors`：参数访问器
- `cmds`：翻译后的 SQL 命令（懒填充）

---

## 执行阶段（Layer 3）

### 执行入口

| 路径 | 说明 |
|------|------|
| `ExpressionQuery` / `EntityProvider` | `IQueryProvider.Execute` → `BasicSentenceRunner` → `SentenceExecutor` |
| `EntityVisitCompiler.DoCompile` | 预编译委托 → `SentenceExecutor.Execute<T>` |
| `CompiledTableT` | `QueryRunner.Cache<T>` 命中后 → `RunnerContextFactory.Create` → `loadElement` |

默认 Runner 为 `BasicSentenceRunner`，**不再注册 Mapper**，直接委托 `SentenceExecutor`：

```csharp
DefaultGetElement           → SentenceExecutor.ExecuteObject(bag, db, expression, parameters)
DefaultGetElementAsync      → SentenceExecutor.ExecuteObjectAsync(...)
DefaultGetResultEnumerable  → StreamingResultEnumerable（无 LoadWith）/ MaterializedResultEnumerable
```

`RunnerContextFactory` 统一构造 `RunnerContext`；`RunnerContext.premble` / `SentenceBag.ExecuteType` 已移除。

### SentenceExecutor 流程

```
EnsureInsertOrUpdateExpanded(bag)   // 不支持原生 upsert 时展开为 2～3 步
FinalizeBag(bag)
  → SqlOptimizerFactory.Get(db).Finalize(statement)
  → SqlProviderHelper.IsValidQuery(...)

BuildSqlBuilder(bag, expression, parameters)
  → QueryMate.SetParameters(...)
  → ClauseTranslateVisitor.Visit(statement)
  → SQLBuilderClause.Builder

Execute
  → IEnumerable<T>  : kit.query<T>().ToList() + NavColumnLoader
  → Scalar (int/bool) : kit.count()
  → DML             : PrepareCommands → ExeNonQuery / 多语句策略
  → InsertOrUpdate  : MySQL 原生 ON DUPLICATE KEY UPDATE（IsInsertOrUpdateSupported）
```

### 导航属性加载

`LoadWith` 在编译阶段向 `SentenceBag.NavColumns` 注册导航列；主查询执行后由 `NavColumnLoader.LoadNavChilds` 发起二次 IN 查询，支持多级 LoadWith 与 `HashSet<Type>` 循环引用检测。

### QueryRunner 保留职责

`QueryRunner` 已精简，仅保留：

- 查询缓存（`Cache<T>` / `Cache<T,TR>`）
- `CompiledTableT` 预编译路径

**已移除：** `BasicResultEnumerable`、`WrapMapper`、`ExecuteElement`、`SetScalarQuery` / `SetNonQueryQuery*`、`GetSqlText` 静态方法、DbDataReader Mapper 链。

---

## 代码目录

```
ext/src/linq/
├── core/                    # 对外编译器入口
│   EntityVisitCompiler.cs
│   EntityQueryCompiler.cs
├── translator/              # ★ 新三层架构核心
│   ClauseCompiler.cs        # 编译收尾 → SentenceBag
│   ClauseExpressionVisitor.cs
│   ClauseMethodVisitor*.cs  # 方法分发（含内联 + Bindings）
│   ClauseMethodVisitor.MooExt.cs / .Async.cs
│   ClausePredicateVisitor.cs / ClauseFieldVisitor.cs
│   ClausePredicateVisitor.cs
│   SentenceExecutor.cs      # Statement → SQLBuilder → 执行
│   NavColumnLoader.cs       # LoadWith 二次加载
│   SqlOptimizerFactory.cs
│   tools/
│       gen_bindings.py      # 生成 Bindings.cs
│       remove_setrunquery.py  # 批量清理 SetRunQuery override
└── src/
    ├── clause/              # SQL 语句结构（SelectQueryClause 等）
    │   ├── helps/           # QueryHelper、优化辅助
    │   └── visitors/        # 语句级 Visitor（优化、校正）
    ├── linq/                # 面向用户的 LINQ 核心
    │   ├── builder/         # ClauseSqlTranslator + IBuildContext + Builder 体系
    │   ├── expressons/      # ExpressionQuery、EntityProvider
    │   ├── query/           # SentenceBag、BasicSentenceRunner、QueryMate
    │   └── Translation/     # 方言函数翻译
    ├── entity/              # 实体映射
    │   ├── mapping/         # 特性 → EntityTable / EntityColumn
    │   └── metadata/
    └── outcast/             # 历史 API、QueryRunner 缓存、Sql 扩展方法
```

---

## 与旧设计对比

### 旧流程（Phase 1）

```
Expression
  → BuildSequence（编译 Statement）
  → BuildQuery（第二编译：投影 + BuildMapper）
  → SetRunQuery（Context 绑定 DbDataReader Mapper）
  → InitPreambles（前置查询）
  → QueryRunner.BasicResultEnumerable（逐行 Map）
```

### 新流程（Phase 2，当前）

```
Expression
  → StatementCompileSession + ClauseCompiler（仅编译 Statement）
  → SentenceBag
  → SentenceExecutor（Statement → SQLBuilder → query<T>）
  → NavColumnLoader（LoadWith）
```

### 关键类型职能变更

| 类型 | 旧职能 | 新职能 |
|------|--------|--------|
| `ClauseSqlTranslator` | 编译 + Mapper 生成 | **仅 SQL 语义**（`TryBuildSequence` 入口之一） |
| `IBuildContext` | 编译 + `SetRunQuery` 绑定执行 | **仅编译**（`MakeExpression` / `GetResultStatement`） |
| `SentenceBag` | 持有 Mapper 委托、Preambles | 持有 Statement + NavColumns + Runner |
| `QueryRunner` | DbDataReader 执行 + Mapper | 缓存 + DML 特殊路径 + GetSqlText |
| `IQueryRunner` | 运行时 Reader 包装 | 保留接口，新路径不再使用 Mapper |

---

## 设计原则

1. **Compile / Execute 分离** — Statement 是可序列化、可缓存、可 inspect 的中间产物；执行策略可独立替换。
2. **复用 Builder 业务逻辑** — 重构只改分发层（Visitor），不重写 Where/Join/GroupBy 等 Builder。
3. **映射交给 Pure** — `SQLBuilder.query<T>()` 是实体映射的唯一路径，避免双轨 Mapper。
4. **最小破坏迁移** — `ISentenceRunner.whenGetElement` 保留，Scalar/NonQuery 等特殊语句仍可通过 Runner 覆盖。
5. **Ext 不重复 Pure** — 方言、连接、命令执行均在 Pure / `DBInstance` 层。

---

## 清理进度

### 阶段 1 — 死代码清理（已完成）

| 项 | 状态 | 说明 |
|----|------|------|
| Preamble 体系 | ✅ | 删除 `preamble/*.cs` |
| Mapper / ConvertFromDataReader 链 | ✅ | 删除 `Mapper.cs`、`ConvertFromDataReaderExpression.cs`、`SequentialAccessHelper.cs` |
| LimitResultEnumerable | ✅ | 已删除 |
| SetScalarQuery / SetNonQueryQuery | ✅ | 从 `QueryRunner` 移除（保留 InsertOrReplace 用的 Query2 路径） |
| QueryRunnerParam / DataReaderParam | ✅ | 从 `ExpressionBuilder` 移除 |
| SentenceBag.finalExp | ✅ | 已移除，参数解析仅用 `srcExp` |
| redo/ 实验代码 | ✅ | 删除 `redo/builder/*.cs` |
| 过时文档 | ✅ | `linq/README.md` 指向本文 |

**阶段 4 已完成：** 原 `toClean/` 内容已迁至 `src/dataprovider/`、`src/sqlquery/`、`src/data/`、`src/async/` 并删除目录。

---

### 阶段 2 — 高频 Builder 内联（已完成）

| 类别 | 状态 | Visitor 文件 |
|------|------|--------------|
| Where / Having | ✅ | `ClauseMethodVisitor.Where.cs` |
| Select | ✅ | `ClauseMethodVisitor.Select.cs` |
| OrderBy* / ThenOrBy* | ✅ | `ClauseMethodVisitor.OrderBy.cs` |
| Take / Skip | ✅ | `ClauseMethodVisitor.TakeSkip.cs` |
| Join* | ✅ | `ClauseMethodVisitor.Join.cs` |
| Distinct / SelectDistinct | ✅ | `ClauseMethodVisitor.Distinct.cs` |
| All / Any | ✅ | `ClauseMethodVisitor.AllAny.cs` |
| Contains | ✅ | `ClauseMethodVisitor.Contains.cs` |
| GroupBy | ✅ | `ClauseMethodVisitor.GroupBy.cs` |
| GroupJoin | ✅ | `ClauseMethodVisitor.GroupJoin.cs` |
| SelectMany | ✅ | `ClauseMethodVisitor.SelectMany.cs` |
| First / Single* | ✅ | `ClauseMethodVisitor.FirstSingle.cs` |
| LoadWith* | ✅ | `ClauseMethodVisitor.LoadWith.cs` |
| Count / Sum / Min / Max / Average | ✅ | `ClauseMethodVisitor.Aggregate.cs` |
| DefaultIfEmpty / OfType / ElementAt* | ✅ | 各对应 partial |
| DML 主入口内联 | ✅ | `ClauseMethodVisitor.Dml.cs`（Insert/Update/Delete/InsertOrUpdate） |
| DML 变体 | ⏳ | InsertWithOutput 等仍走 `ApplyBuilder` |
| Merge / SetOp | ✅ | `ClauseMethodVisitor.Merge.cs` / `SetOp.cs`；`SetOperationBuilder.Compile` / `MergeBuilder.Compile` |

内联后运行 `python ext/src/linq/translator/tools/gen_bindings.py` 同步 `ClauseMethodVisitor.Bindings.cs`。

**接口清理：** `IQueryRunner` 已移除 `Preambles`、`MapperExpression`。

**ExpressionBuilder 主文件** 现约 **569 行**（Where/Take/Helpers/CTE 等）；Projection / BuildExpression / Predicate 等已拆至 partial。

---

### 阶段 3 — 执行层统一（已完成）

| 项 | 状态 | 说明 |
|----|------|------|
| DML 迁入 `SentenceExecutor` | ✅ | `SentenceExecutor.Dml.cs`：Insert/Update/Delete + InsertOrUpdate 两步策略 |
| `QueryRunner` 精简 | ✅ | 仅保留 `Cache<T>` / `ClearCaches`；删除 DML 与 `InsertOrReplace` |
| `GetAsyncEnumerator` | ✅ | 委托 `loadResultList` → `MaterializedResultEnumerable` |
| `ExecuteObjectAsync` | ✅ | 写操作走 `ExeNonQueryAsync`；查询走 `queryAsync` / `queryUniqueAsync` |
| `GetSqlText` | ✅ | 支持多语句（InsertOrUpdate 展开后拼接 SQL） |

---

### 阶段 4 — 目录整理（已完成）

| 项 | 状态 | 说明 |
|----|------|------|
| 删除 `toClean/` | ✅ | 运行时代码迁至正式目录 |
| `SqlProviderHelper` / `IQueryParametersNormalizer` | ✅ | → `src/dataprovider/` |
| `ReservedWords` + txt | ✅ | → `src/sqlquery/` |
| `DataParameter` / `EntityConstructorBase` | ✅ | → `src/data/` |
| `AsyncExtensions` | ✅ | → `src/async/` |
| `QueryRunner.Cache` | ✅ | → `src/linq/query/QueryRunner.Cache.cs` |
| 删除 `redo/builder/` | ✅ | 无引用的实验 Visitor |
| `ExpressionBuilder.SqlBuilder.Projection.cs` | ✅ | 自 SqlBuilder 拆出 Projection（~710 行） |
| `ExpressionBuilder.SqlBuilder.BuildExpression.cs` | ✅ | BuildExpression（~1176 行） |
| `ExpressionBuilder.SqlBuilder.Predicate.cs` | ✅ | Predicate Converter 主体（~878 行） |
| `ExpressionBuilder.SqlBuilder.ConvertCompare.cs` | ✅ | ConvertCompare（~847 行） |

**SqlBuilder 主文件** 现约 **569 行**（Where/Take/Helpers/CTE 等）。

**保留：** `outcast/` 下 `LinqExtensions`、`Sql` 等仍为公共 API，后续可 rename 为 `publicapi/`。

---

## 实现进度总览（2026-06 比对）

Phase 2 三层架构（Compile → SentenceBag → Execute）**主骨架已落地**。`MakeExpression` 已从 linq2db 移植；SQLite 端到端 Where 查询已验证。

| 层级 | 状态 | 说明 |
|------|------|------|
| Layer 1 Compile | ✅ | 双访问器 + `ClauseCompiler`；`MakeExpression` 已移植 |
| Layer 2 SentenceBag | ✅ | 语句包、NavColumns、IsCacheable |
| Layer 3 Execute | ✅ | SentenceExecutor → ClauseTranslateVisitor → SQLBuilder.query&lt;T&gt;() |

**集成测试（SQLite）：** `LinqSqliteTestFixture` + `LinqCompileTests`（25 项：Where/Like/LikeLeft/OrderBy/Take/Count/关联 to-one 编译与执行已启用）。

---

## 未来计划

### 阻塞项（编译闭环）

- [x] **`ExpressionBuilder.MakeExpression`** — 自 linq2db 移植至 `ExpressionBuilder.SqlBuilder.MakeExpression.cs`
- [x] **`GetSubQuery` / `TryGetSubQueryExpression`** — 子查询表达式解析
- [x] **`TryCreateAssociation` 完整实现** — FK to-one 在不支持 APPLY 方言上编译为 `INNER JOIN`（非 `CROSS APPLY`）；`EntityVisit_AssociationToOne_*` 已通过
- [ ] **`ExpressionBuilder.GetContext`** — linq2db 亦为 stub，低优先级
- [x] **端到端 LINQ 编译测试** — SQLite：`useBus1`、`EntityVisit_Where_ExecutesAgainstSqlite`；`LinqCompileTests` Where SqlText/SelectQuery 结构断言

### 短期

- [x] 统一 `GetSqlText` / `TranslateCmds` 参数传递（`RunnerContextFactory`、`ParameterValues`）
- [x] InsertOrUpdate 方言原生 UPSERT（MySQL `ON DUPLICATE KEY UPDATE` / SQLite `ON CONFLICT`）
- [x] DML `Compile()` 内联：`ClauseMethodVisitor.Dml.cs`
- [x] **LinqCompileTests 扩展** — Where/OrderBy/Take/Count 编译与执行（含 `Where(u => u.IsActive).Count()`）
- [x] **ClauseCompiler 根 SelectQuery** — 不再使用 `QueryPool` 作为最终 Statement 载体，避免 `Cleanup()` 清空 ORDER BY / TAKE
- [x] **同步 `core/*.md`** — Phase 2：`ClauseCompiler`、`SentenceExecutor`、无 Mapper/Preambles

### 中期（架构完善）

- [x] 继续拆分 `ExpressionBuilder.SqlBuilder`（SearchCondition / Projection / Predicate 等 partial）
- [ ] `outcast/` rename → `publicapi/`（待批量迁移）
- [x] `StreamingResultEnumerable`（⚠️ 同步路径仍 `ToList()`，非 DB 级流式）
- [ ] **Take/Skip 落地完善** — `VisitSelectClause` 已有 top/setPage；缺 offset/limit、Skip-only、动态参数
- [ ] **Take/Skip 客户端截断** — Jet 等无 OFFSET 方言 fallback
- [x] 编译缓存：`SentenceBag.IsCacheable` + `ExpressionQuery.Info`
- [x] `NavColumnLoader`：多级 LoadWith、循环引用检测
- [x] Merge / SetOp 内联 — `ClauseMethodVisitor.Merge.cs` / `SetOp.cs`；Bindings 已移除对应 `ApplyBuilder`

#### Take/Skip 专项

| 项 | 状态 | 说明 |
|----|------|------|
| 仅 Take → `top(n)` | ✅ | `ClauseTranslateVisitor.VisitSelectClause` |
| Skip+Take 对齐 → `setPage` | ⚠️ | 仅 skip%take==0 正确 |
| Skip+Take 非对齐 → OFFSET+LIMIT | ❌ | 需 SQLBuilder API |
| 仅 Skip | ❌ | 未处理 |
| UI 页码 floor(10.2→10) | 文档 | 放在 `setPage` 入参层，非 LINQ Skip |

### 长期（能力与生态）

- [x] 编译阶段产出 **可 inspect 的 SQL 计划**（`SqlPlan` / `StatementStructure`，`translator/plan/`）
- [x] **Statement 级单元测试** — `StatementStructureTests` 通过 `LinqStatementCompiler` 断言结构，不执行 SQL
- [ ] 与 **SQLClip / SQLBuilder** 链式 API 互操作（Expression ↔ SQLBuilder 双向）
- [ ] **多语句事务批处理** — `SentenceBag.Sentences.Count > 1` 统一执行器
- [x] **translator/ 独立模块** — `LinqStatementCompiler.Compile` 公开入口（`ext/src/linq/translator/`）
- [ ] **真异步流式** — `IAsyncEnumerable` 逐条读库
- [ ] **方言 Take/Skip 能力矩阵** — SQL 下推 / ROW_NUMBER / 客户端截断策略文档化

### 建议执行顺序

```
MakeExpression + GetSubQuery（已完成）
  → 端到端 SQLite 测试（useBus1 / LinqCompileTests）✅
  → OrderBy / Take / Count / 关联 to-one 编译与执行链 ✅
  → TryCreateAssociation
  → Take/Skip 落地 + 客户端截断策略
  → outcast rename
  → 长期项（SqlPlan / SQLClip 互操作 / 多语句事务）
```

**参考源码：** `h:\coding\gitee\ORM\linq2db-master\Source\LinqToDB\Linq\Builder\`

---

## 原「未来计划」归档

<details>
<summary>早期 checkbox 列表（已被上文取代）</summary>

### 短期（已完成项）

- [x] 统一参数传递
- [x] LinqCompileTests 基础设施
- [x] InsertOrUpdate UPSERT
- [x] DML Compile 内联

### 中期

- [x] SqlBuilder 拆分
- [ ] outcast rename
- [x] StreamingResultEnumerable
- [ ] Take/Skip 客户端截断
- [x] 编译缓存
- [x] NavColumnLoader

### 长期

- [x] SqlPlan
- [x] Statement 测试
- [ ] SQLClip 互操作
- [ ] 多语句事务
- [x] translator 独立模块

</details>

---

## 调试提示

```csharp
// 公开 API：编译为 SqlPlan（不执行）
var result = LinqStatementCompiler.Compile(db, queryable.Expression);
var structure = result.PrimaryStructure; // HasWhere, Joins, TakeValue…
var sql = result.Plan.SqlPreview;

// 内部调试（同程序集）
var bag = QueryMate.GetQuery<MyEntity>(db, ref expression, out _);
// bag.Sentences[0].Statement → SelectQueryClause
// bag.NavColumns → LoadWith 注册列
```

Activity 追踪 ID 见 `ActivityID`：`BuildSequence`、`FinalizeQuery`、`ExecuteQuery`、`GetSqlText`、`Materialization` 等。

---

## 相关文档

- [双访问器对齐 FastLinq 迁移清单](../双访问器对齐FastLinq-迁移清单.md) — 分发层对齐 FastLinq 的 Phase A～F
- `ext/src/linq/core/ClauseCompiler-构建SentenceBag解析.md` — 编译过程（`ClauseCompiler` + `StatementExpression`，无 BuildQuery/Mapper）
- `ext/src/linq/core/EntityVisitCompiler-执行过程解析.md` — 执行入口（Phase 2：`SentenceExecutor`）
- `pure/src/ado/builder/API说明文档.md` — SQLBuilder 链式 API
