# mooSQL.Ext LINQ 子项目

## 项目说明

本子项目是对 **mooSQL.Pure** 核心库的增强层，主要提供：

- 基于实体类的 **LINQ 表达式**查询（`IQueryable<T>`）
- 表达式树 → SQL 语句结构的编译
- 编译结果通过 Pure 层 `SQLBuilder` / `ClauseTranslateVisitor` 执行
- 实体映射、导航属性（`LoadWith`）、方言扩展等衍生能力

**边界原则：** 本子项目 **不包含** 数据库连接、方言实现、SQL 执行器等与 Pure 重合的能力；执行一律委托给 `DBInstance` + Pure 层基础设施。

---

## 设计目标

### 问题背景

早期 `ExpressionBuilder` 是一个「巨型类」，同时承担：

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
│  ExpressionBuilder.TryBuildSequence                              │
│    → ClauseExpressionVisitor + ClauseMethodVisitor               │
│    → IBuildContext / ISequenceBuilder（既有 Builder 逻辑）        │
│    → ClauseCompiler.Compile → SentenceBag                        │
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
| `ExpressionBuilder.doBuild<T>()` | 内部编译，产出 `SentenceBag<T>` |
| `QueryMate.GetQuery<T>()` | 带缓存的对外入口 |
| `EntityVisitCompiler` / `EntityQueryCompiler` | 预编译委托，直接调用 `SentenceExecutor` |

### 双访问器模型

编译 MethodCall 链时使用 **Buddy 双访问器**，替代原先在 `ExpressionBuilder` 内硬编码的分发：

```
MethodCallExpression
    → CallUntil.CreateCall(node)        // 强类型 MethodCall 节点
    → ClauseMethodVisitor.VisitXxx      // 按方法名分发
    → ISequenceBuilder / IBuildContext  // 复用既有 Builder 业务逻辑
```

| 组件 | 职责 |
|------|------|
| `ClauseExpressionVisitor` | 遍历表达式树；MethodCall 转 Call 节点；非 Call 节点走 `SequenceBuilderResolver` |
| `ClauseMethodVisitor` | 按 LINQ 方法名 VisitXxx，内联或 ApplyBuilder 到既有 Builder |
| `ClauseCompileContext` | 编译上下文（ExpressionBuilder + BuildInfo + BuildResult） |
| `SequenceBuilderResolver` | 非 Call 节点（Constant / Member / Lambda 等）的 Builder 查找 |
| `ClauseCompiler` | 编译收尾：收集 Statement、参数、NavColumns → `SentenceBag` |

### MethodCall 分发策略

```
MethodCall
  ├─ 已内联 VisitXxx（高频、逻辑稳定）
  │     Where / Having / Select / OrderBy* / Take / Skip / Join*
  ├─ ApplyBuilder<T>（显式绑定 ISequenceBuilder，~120 个方法）
  │     见 ClauseMethodVisitor.Bindings.cs（由 gen_bindings.py 生成）
  ├─ DispatchPassThrough（AsQueryable / Alias 等透传）
  └─ DispatchLegacy → SequenceBuilderResolver（扩展方法等兜底）
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
- `ExpressionBuilder.MakeExpression` / `ConvertToSql`：表达式 → SQL 片段

`TryBuildSequence` 流程：

```
ExpandToRoot(expression)
  → MethodCall? ClauseExpressionVisitor + ClauseMethodVisitor
  → 其他节点? SequenceBuilderResolver.FindBuilder → BuildSequence
  → 得到 IBuildContext（含 SelectQueryClause）
  → ClauseCompiler 收集 GetResultStatement() → SentenceItem
```

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
| `ExpressionQuery` / `EntityProvider` | `IQueryProvider.Execute` → `Runner.loadElement` |
| `EntityVisitCompiler.DoCompile` | 预编译委托 → `SentenceExecutor.Execute<T>` |
| `QueryRunner.SetScalarQuery` 等 | INSERT/UPDATE/Scalar 等特殊语句（保留） |

默认 Runner 实现为 `BasicSentenceRunner`，**不再注册 Mapper**，直接委托 `SentenceExecutor`：

```csharp
DefaultGetElement      → SentenceExecutor.ExecuteObject
DefaultGetResultEnumerable → SentenceExecutor.ExecuteList → MaterializedResultEnumerable<T>
```

`RunnerContext` 必须携带 `sentenceBag`；`dataContext` / `expression` 可省略（回退到 bag 内字段）。

### SentenceExecutor 流程

```
FinalizeBag(bag)
  → SqlOptimizerFactory.Get(db).Finalize(statement)   // 方言优化
  → SqlProviderHelper.IsValidQuery(...)               // 合法性校验

BuildSqlBuilder(bag)
  → QueryMate.SetParameters(...)
  → ClauseTranslateVisitor.Visit(statement)
  → SQLBuilderClause.Builder

Execute
  → IEnumerable<T>  : kit.query<T>().ToList() + NavColumnLoader
  → Scalar (int/bool) : kit.count()
  → 其他 Scalar       : kit.queryUnique<T>() / queryScalar
```

### 导航属性加载

`LoadWith` 在编译阶段向 `SentenceBag.NavColumns` 注册导航列；主查询执行后由 `NavColumnLoader.LoadNavChilds` 发起二次 IN 查询填充导航属性（逻辑自 FastLinq 移植）。

### QueryRunner 保留职责

`QueryRunner` 已大幅精简，仅保留：

- 查询缓存（`Cache<T>` / `Cache<T,TR>`）
- `FinalizeQuery`（非 SELECT 路径）
- `SetScalarQuery` / `SetNonQueryQuery` / `SetNonQueryQuery2` / `SetQueryQuery2`（DML 特殊执行）
- `GetSqlText` → 委托 `SentenceExecutor.GetSqlText`

**已移除：** `BasicResultEnumerable`、`WrapMapper`、`ExecuteElement`、`GetExecuteQuery`、全部 `SetRunQuery` 静态方法及 DbDataReader Mapper 链。

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
│   SequenceBuilderResolver.cs
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
    │   ├── builder/         # ExpressionBuilder + IBuildContext + Builder 体系
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
  → TryBuildSequence + ClauseCompiler（仅编译 Statement）
  → SentenceBag
  → SentenceExecutor（Statement → SQLBuilder → query<T>）
  → NavColumnLoader（LoadWith）
```

### 关键类型职能变更

| 类型 | 旧职能 | 新职能 |
|------|--------|--------|
| `ExpressionBuilder` | 编译 + Mapper 生成 | **仅编译**（`doBuild` / `TryBuildSequence`） |
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
| DML / Merge / SetOp 等 | ⏳ | 仍走 `ApplyBuilder` + Bindings 生成 |

内联后运行 `python ext/src/linq/translator/tools/gen_bindings.py` 同步 `ClauseMethodVisitor.Bindings.cs`。

**接口清理：** `IQueryRunner` 已移除 `Preambles`、`MapperExpression`。

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

**保留：** `outcast/` 下 `LinqExtensions`、`Sql` 等仍为公共 API，后续可 rename 为 `ext/`。

---

## 未来计划

### 短期

- [ ] 统一 `GetSqlText` / `TranslateCmds` 参数传递（`Parameters` 字段与 expression 内嵌参数的一致性）
- [ ] 补充集成测试：First/Single/Count、Join、LoadWith、InsertOrUpdate、DML
- [ ] InsertOrUpdate 方言原生 MERGE/UPSERT 转译（`VisitInsertOrUpdateSentence`）

### 中期（架构完善）

- [ ] 继续拆分 `ExpressionBuilder.SqlBuilder`（Predicate / ConvertCompare 等区段）
- [ ] `outcast/` rename → `ext/`（LinqExtensions、Sql 公共 API）
- [ ] 异步流式枚举：`ExecuteAsyncEnumerable` 避免全量 `ToList` 物化
- [ ] Take/Skip 在不支持方言上的客户端截断评估
- [ ] 编译缓存策略：`QueryRunner.Cache<T>` 与 `ClauseCompiler` 产物对齐
- [ ] 完善 `NavColumnLoader`：集合导航、多级 LoadWith、循环引用检测

### 长期（能力与生态）

- [ ] 编译阶段产出 **可 inspect 的 SQL 计划**（类似 `EXPLAIN` 元数据），供调试 UI 使用
- [ ] Statement 级别单元测试：不连 DB 即可断言 `SelectQueryClause` 结构
- [ ] 与 SQLClip / SQLBuilder 链式 API 互操作（Expression → SQLBuilder 双向）
- [ ] 多语句事务批处理（`SentenceBag.Sentences.Count > 1` 的统一执行器）
- [ ] 考虑将 `translator/` 提升为独立模块，供非 LINQ 场景复用 Statement 编译

---

## 调试提示

```csharp
// 查看生成的 SQL（DEBUG 模式下 ExpressionQuery 暴露 _sqlText）
var sql = queryable.SqlText;

// 编译产物
var bag = QueryMate.GetQuery<MyEntity>(db, ref expression, out _);
// bag.Sentences[0].Statement → SelectQueryClause
// bag.NavColumns → LoadWith 注册列
```

Activity 追踪 ID 见 `ActivityID`：`BuildSequence`、`FinalizeQuery`、`ExecuteQuery`、`GetSqlText`、`Materialization` 等。

---

## 相关文档

- `ext/src/linq/core/ExpressionBuilder-构建SentenceBag解析.md` — 编译过程详细解析（部分描述已随 Phase 2 过时，以本文为准）
- `ext/src/linq/core/EntityVisitCompiler-执行过程解析.md` — 执行入口解析
- `pure/src/ado/builder/API说明文档.md` — SQLBuilder 链式 API
