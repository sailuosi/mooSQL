---
name: Refactor-EntityQueryCompiler-to-Fast-Style
overview: Plan to refactor EntityQueryCompiler so that it uses a FastLinqCompiler-like expression parsing pipeline while preserving its richer feature set and tighter integration with mooSQL's ExpressionBuilder/QueryRunner.
todos:
  - id: analyze-base-flow
    content: Review BaseQueryCompiler, FastLinqCompiler, EntityQueryCompiler, and EntityQueryProvider flow to fully understand current compile pipeline.
    status: pending
isProject: false
---

## 目标与现状对比

- **目标**：让 `EntityQueryCompiler` 使用与 `FastLinqCompiler` 相似的“表达式 → 访问器 → SQL/执行委托”路径，同时保留/增强其与 `mooSQL.linq` 中 `ExpressionBuilder`、`QueryRunner` 的紧密集成和高级特性。
- **当前 Fast 路径**（已投入使用，方法支持较少）：
  - 入口：`[pure/src/linq/basis/EntityQueryProvider.cs]` → `_queryCompiler.Execute<TResult>(expression)`。
  - 编译器：`[pure/src/linq/fast/FastLinqCompiler.cs]`：
    - `DoCompile<TResult>(Expression expression, QueryContext context)`
    - 创建 `FastMethodVisitor` + `FastExpressionTranslatVisitor`，双向 Buddy。
    - 创建并初始化 `FastCompileContext<TResult>`，内部持有 `SQLBuilder` 与当前 Layer。
    - `wok.Visit(expression)` 遍历表达式树，调用 fast 访客和各类 *Call*，写入 `SQLBuilder` 并设定 `onExecute` 或 `onRunQuery`。
    - 返回 `Func<QueryContext, TResult>`：优先 `onExecute`，否则根据 `onRunQuery` 或 `WhenRunQuery<TResult>`。
  - 下游：`SQLBuilder` 直接构造 SQL，执行路径与 `mooSQL.data` 中的 builder/执行器对接。
- **当前 Entity 路径**（特性丰富，但风格不同）：
  - 入口同样是 `EntityQueryProvider` + `LinqDbFactory`，通过 `[ext/src/linq/core/EntityLinqFactory.cs]` 决定编译器为 `EntityQueryCompiler`。
  - 编译器：`[ext/src/linq/core/EntityQueryCompiler.cs]`：
    - `DoCompile<TResult>(Expression expression, QueryContext context)` 中直接调用 `QueryMate.GetQuery<TResult>(DB, ref expression, out depon)`。
    - 得到一个 `SentenceBag<TResult>` 风格的 `query`，内部封装了 `ExpressionBuilder` 翻译、SQL 语句和 `QueryRunner`。
    - 返回的委托里，在每次执行时调用 `query.InitPreambles` → `query.Runner.loadElement` 或 `loadElementAsync`。
  - Query/执行细节由 `[ext/src/linq/src/outcast/root/core/runner/QueryRunner.cs]` 以及 `[ext/src/linq/src/linq/builder/expressionBuilder/*]` 管理。

## 总体重构思路

- **保持接口与工厂不变**：`IQueryCompiler`、`BaseQueryCompiler`、`EntityQueryProvider`、`LinqDbFactory` 派生类 (`FastLinqFactory`、`EntityLinqFactory`) 的对外接口保持兼容，只在 `EntityQueryCompiler.DoCompile` 内部替换实现。
- **借鉴 Fast 流程，而非简单复制**：
  - 延续 Fast 编译器的“**一次编译，返回执行委托**”模型：在 `DoCompile` 阶段完成表达式分析、SQL 构建和 `RunnerContext` 生成策略。
  - 但 **不替换 Entity 现有的 `ExpressionBuilder` / `SentenceBag` / `QueryRunner` 管线**，而是在 Fast 风格的访问器中调用这些组件。
- **分两层抽象**：
  - 上层：`EntityQueryCompiler` 负责整体编译流程（Prepare → Visit → 构造委托）。
  - 下层：新的 Entity 风格 Visitor + Context（类似 `FastMethodVisitor` + `FastCompileContext`），负责将 LINQ 表达式分解到 `QueryMate` / `ExpressionBuilder` / `QueryRunner` 所需的调用形态。
- **增量迁移**：先只接入「查询基本子集」（Where/Select/OrderBy/Take/Skip/Single/ToList），确保表现与原 Entity 实现一致，再逐步迁移高级功能。

## 分阶段详细计划

### 阶段 1：梳理与抽象公共编译接口

- **1.1 理清 Base 流程**
  - 通读 `[pure/src/linq/basis/BaseQueryCompiler.cs]`（如存在），确认 `Execute`、`ExecuteAsync`、`PrepareExpression`、`DoCompile` 的模板方法流程和扩展点。
  - 对比 `FastLinqCompiler.DoCompile` 与 `EntityQueryCompiler.DoCompile` 现有签名，列出两者在：
    - 是否缓存编译结果
    - 是否重写 `PrepareExpression`
    - 是否处理异步场景（`cancellationToken` 使用）
    - 对 `QueryContext` 的使用方式
    方面的差异。
- **1.2 提炼“编译上下文”抽象**
  - 在 `pure/src/linq/fast/compile` 下复习 `FastCompileContext<TResult>` 的职责（当前 Layer、onRunQuery/onExecute、实体类型、导航列等）。
  - 设计一个 `**ICompileContext` 或抽象基类**，抽象出：
    - 当前实体类型/结果类型
    - 记录“执行委托”的方式（`onExecute` / `onRunQuery` 或等价结构）
    - 存储 SQL 构建器 / `SentenceBag` / `ExpressionBuilder` 入口
  - 确保该抽象不强绑定 `SQLBuilder`，便于 Entity 分支在内部用 `SentenceBag` + `ExpressionBuilder`。

### 阶段 2：分析 Entity 查询管线（QueryMate / ExpressionBuilder / QueryRunner）

- **2.1 QueryMate 视角**
  - 阅读：
    - `[ext/src/linq/src/linq/query/Query.until.cs]`
    - `[ext/src/linq/src/linq/expressons/queryable/ExpressionQuery.cs]`
    - `[ext/src/linq/src/linq/expressons/provides/EntityProvider.cs]`
  - 弄清 `QueryMate.GetQuery<TResult>(DB, ref expression, out depon)`：
    - 如何从 `Expression` 构造 `SentenceBag<TResult>`。
    - 何处调用 `ExpressionBuilder.doBuild<T>`、`ExpressionBuilder.BuildMapper<T>`。
    - 查询缓存（`QueryRunner.Cache<T>`）与 `QueryMate` 之间的关系。
- **2.2 ExpressionBuilder 视角**
  - 关注 `[ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.cs]` 及 `*.SqlBuilder.cs`：
    - `doBuild<T>()`：如何从 `Expression` → `SentenceBag<T>`（内部用 `BuildSequence`、`BuildQuery`、`SelectQueryClause` 等）。
    - `BuildMapper<T>`：如何从 `SelectQueryClause` 生成 DataReader → T 的映射委托。
  - 记录当前 Entity 管线中“表达式解析”的关键阶段（`ConvertExpressionTree`、`BuildSequence`、`FinalizeProjection` 等），以便后续对齐到 Fast 风格的“访问器 + Context”。
- **2.3 QueryRunner 视角**
  - 在 `[ext/src/linq/src/outcast/root/core/runner/QueryRunner.cs]` 中梳理：
    - `loadElement` / `loadElementAsync` 接受的 `RunnerContext` 字段含义（`dataContext`、`expression`、`sentenceBag`、`preamble` 等）。
    - 如何利用 `SentenceBag` + `ExpressionBuilder.BuildMapper` 执行 SQL 和映射结果。

### 阶段 3：设计 Entity 风格的“Fast 访问器”层

- **3.1 新 Context：`EntityCompileContext<TResult>**`
  - 放在例如 `[ext/src/linq/core/compile/EntityCompileContext.cs]`：
    - 属性：
      - `DBInstance DB`
      - `Type EntityType` / `Type ResultType`
      - `SentenceBag<TResult> Query`（或泛型 `SentenceBag`，视 `QueryMate` 返回类型而定）
      - `Func<QueryContext, TResult>? OnExecute`
      - `Func<RunnerContext, object>? OnRunQuery`（或直接 `Func<QueryContext, TResult>` 以简化）
    - 方法：
      - 初始化 `Query` 的策略（例如：在第一次需要时才调用 `QueryMate.GetQuery`）。
      - 帮助方法：`SetScalarExecutor`、`SetSequenceExecutor`、`SetNonQueryExecutor` 等，用于与 `QueryRunner` 对接。
- **3.2 新 Expression Visitor：`EntityExpressionTranslateVisitor**`
  - 类似 `FastExpressionTranslatVisitor`：
    - 继承 `BaseTranslateVisitor` 或 `ExpressionVisitor`。
    - 负责把 `Expression` 中的 `MethodCallExpression` 转换为 `MethodCall`（或者直接派发到 Entity 版 MethodVisitor）。
    - 复用已有 `CallUntil.CreateCall` 和 `MethodCallFactory`，减少重写工作。
- **3.3 新 Method Visitor：`EntityMethodVisitor**`
  - 参考 `FastMethodVisitor` 中的结构，但实现细节改成驱动 `QueryMate` / `ExpressionBuilder`：
    - `VisitWhere`：将条件表达式交给 `ExpressionBuilder.ConvertToSqlExpr` 或相应 helper，而非直接操作 `SQLBuilder`；设置或修改 `SentenceBag`/`SelectQueryClause`。
    - `VisitSelect` / `VisitOrderBy` / `VisitGroupBy` / `VisitTakeSkip`：使用 `ExpressionBuilder` 的 `Project`/`BuildSearchCondition` 等方法。
    - 终结操作（`ToList`、`First/Single`、`Count`、分页）：
      - 设置 `EntityCompileContext.OnExecute` 或 `OnRunQuery`，内部调用 `QueryRunner.loadElement`/`loadElementAsync`、`QueryRunner.GetExecuteQuery<T>` 等。
  - 目标是：**将原本“黑盒”的 `QueryMate.GetQuery` 细分为「逐步构建查询 + 最后执行」的显式流程，风格上接近 Fast 编译器。**

### 阶段 4：重写 EntityQueryCompiler.DoCompile 为 Fast 风格

- **4.1 保留旧实现以便回退**
  - 在 `[ext/src/linq/core/EntityQueryCompiler.cs]` 中：
    - 提取当前 `DoCompile` 实现为私有方法 `DoCompileLegacy<TResult>`（仅重命名，不改逻辑）。
    - 新的 `DoCompile` 内部先调用一个 feature 开关（如 `DB.Options.UseLegacyEntityCompiler`），为平滑迁移预留回退路径。
- **4.2 新 DoCompile 流程**（伪代码级）：
  - 创建 `EntityMethodVisitor` 与 `EntityExpressionTranslateVisitor`，互设 Buddy。
  - 创建 `EntityCompileContext<TResult>` 并挂到 visitor。
  - 调用 `translateVisitor.Visit(expression)`：
    - 在遍历中逐步构建 `SentenceBag` 或 `SelectQuery`；为不同的 LINQ 终止方法设置 `OnExecute` 或 `OnRunQuery`。
  - 若 `OnExecute` 已设置，直接返回；否则用 `OnRunQuery` 包装一个 `Func<QueryContext, TResult>`，类似 Fast 编译器的返回方式。
  - 在 `Execute` 的委托内部，从 `QueryContext` 构造 `RunnerContext` 并调用 `QueryRunner`。

### 阶段 5：与现有 Entity 管线对齐（行为对比与兼容）

- **5.1 对比输入/输出契约**
  - 确认 `QueryMate.GetQuery` 生成的 `query.Runner` 与 `EntityCompileContext` 中所使用的 `QueryRunner` 在：
    - 参数（表达式、参数数组、preambles）
    - 返回值（`TResult`、`IEnumerable<T>`、分页 DTO）
    上必须一致。
- **5.2 行为回归测试**
  - 在 `[mooSQL2024/Tests/src/TestExt/LINQTest.cs]` 及其他已有测试基础上：
    - 为 Fast 与新的 Entity 编译器分别增加覆盖：Where/Select/OrderBy/Take/Skip/Count/Single/分页/导航加载/更新删除（若 Entity 支持）。
    - 比较生成的 SQL（可以通过日志或 debug 输出）与结果集（数据集可固定）。
  - 若存在差异，优先保持 **Entity 原有行为**，在 Visitor 中做兼容处理（例如 null 处理、字符串比较、枚举映射）。

### 阶段 6：分批扩展 Entity 的方法支持与性能优化

- **6.1 方法覆盖梳理**
  - 从 `FastMethodVisitor` 的 `VisitXxx` 集合与 Entity 当前支持的方法（文档与代码）对比：
    - 标记已有但未覆盖的方法（例如某些 Update/Output、Merge、导航加载的组合）。
    - 为 Entity 编译器按优先级实现对应 Visit 方法。
- **6.2 性能与缓存**
  - 引入类似 `FastLinqCompiler` 的编译结果缓存（基于表达式树结构哈希）：
    - `BaseQueryCompiler` 如有 compiled query 机制，按其规范实现。
    - 将 `EntityCompileContext` 作为缓存值，避免每次执行都重新解析表达式。
  - 利用现有的 `QueryRunner.Cache<T>` 与 `ExpressionBuilder` 的内部缓存（`_cachedSql` 等），确保新路径不会破坏现有命中率。

### 阶段 7：清理与文档更新

- **7.1 清理重复实现**
  - 若新 Entity 访问器已完全承担 `QueryMate.GetQuery` 的职责，可以：
    - 逐步将 `QueryMate` 收敛为“兼容层”或删除不再使用的入口。
    - 保留必须的工具函数（如参数绑定、preamble 初始化）。
- **7.2 架构文档同步**
  - 更新 `[doc/docs/moohelp/arch/linq-architecture.md]` 与 `[pure/功能介绍和架构分析.md]`：
    - 在“表达式编译”部分补充 `EntityQueryCompiler` 的新路径，与 `FastLinqCompiler` 形成并列说明。
    - 用一个时序图展示：`EntityQueryProvider` → `EntityQueryCompiler` → `EntityExpressionTranslateVisitor`/`EntityMethodVisitor` → `ExpressionBuilder`/`QueryRunner`。

