## EntityVisitCompiler 执行过程解析

> **Phase 2 更新（2026-06）**  
> 执行入口已统一为 **`SentenceExecutor`**：`BasicSentenceRunner` 默认委托 `SentenceExecutor.ExecuteObject` / `ExecuteList`，实体映射走 **`SQLBuilder.query<T>()`**，不再使用 DbDataReader Mapper / Preambles / `finalExp`。  
> 参数解析统一经 **`RunnerContextFactory`**（`srcExp` + `paras`）。下文 §2、§4–§6 已按 Phase 2 重写；§3 中 SetParameters/GetCommand 仍有效。

本文从一次 LINQ 查询调用开始，追踪 `EntityVisitCompiler` 及其关联组件的执行链路，直到 SQL 执行并返回结果。

---

### 1. 顶层入口：BaseQueryCompiler

相关代码（简化）：

```12:61:pure/src/linq/basis/BaseQueryCompiler.cs
public abstract class BaseQueryCompiler : IQueryCompiler
{
    protected DBInstance DB;

    public BaseQueryCompiler(DBInstance DB) {
        this.DB = DB;
    }

    private QueryContext GetContext() {
        var context = new QueryContext();
        context.DB = DB;
        return context;
    }

    public Func<QueryContext, TResult> CreateCompiledQuery<TResult>(Expression query)
    {
        var context = GetContext();
        var queryNext = PrepareExpression(query);
        var fun = DoCompile<TResult>(queryNext, context);
        return fun;
    }

    public TResult Execute<TResult>(Expression query)
    {
        var context = GetContext();
        var queryNext = PrepareExpression(query);
        var fun = DoCompile<TResult>(queryNext,context);
        return fun(context);
    }

    public TResult ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken)
    {
        var context = GetContext();
        context.cancellationToken = cancellationToken;
        var queryNext = PrepareExpression(query);

        var fun = DoCompile<TResult>(queryNext,context);
        return fun(context);
    }

    public virtual Expression PrepareExpression(Expression expression) {
        return expression;
    }

    public abstract Func<QueryContext, TResult> DoCompile<TResult>(Expression expression,QueryContext context);
}
```

- **职责**：抽象编译器基类，统一了：
  - 创建 `QueryContext`（携带 `DBInstance` 以及可选的 `CancellationToken`）。
  - 执行前的表达式预处理（`PrepareExpression`，当前 `EntityVisitCompiler` 未重写，直接透传）。
  - 把“表达式 + 上下文”委托给具体编译器的 `DoCompile`。
- **相同职责分发点**：
  - `Execute` 与 `ExecuteAsync` 负责“执行 LINQ 查询并得到结果”，但分支依据是**是否需要异步 / 是否提供 CancellationToken**：
    - 同步：`Execute<TResult>(Expression query)`。
    - 异步语义：`ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken)`。
  - 两者最终都走到同一个抽象方法 `DoCompile`，区别在于 `QueryContext.cancellationToken` 是否被设置。

---

### 2. EntityVisitCompiler：编译并委托 SentenceExecutor（Phase 2）

文件：`ext/src/linq/core/EntityVisitCompiler.cs`

```15:26:ext/src/linq/core/EntityVisitCompiler.cs
public override Func<QueryContext, TResult> DoCompile<TResult>(Expression expression, QueryContext context)
{
    var query = QueryMate.GetQuery<TResult>(DB, ref expression, out _);
    query.DBLive = DB;
    query.srcExp = expression;

    return ctx =>
    {
        ctx.DB ??= DB;
        return SentenceExecutor.Execute<TResult>(query, ctx, expression);
    };
}
```

#### 2.1 DoCompile 的职责

1. **`QueryMate.GetQuery<TResult>`** — 编译 LINQ 表达式为 `SentenceBag<TResult>`（见 ExpressionBuilder 文档）。
2. **返回执行委托** — 直接调用 **`SentenceExecutor.Execute<TResult>`**，不再：
   - ~~`InitPreambles`~~
   - ~~在编译期配置 `whenGetElement` / Mapper~~

#### 2.2 同步 / 异步

- **同步**：`BaseQueryCompiler.Execute` → 上述委托 → `SentenceExecutor.Execute`
- **异步**：`ExpressionQuery` / `BasicSentenceRunner.DefaultGetElementAsync` → `SentenceExecutor.ExecuteObjectAsync`
- `EntityVisitCompiler` 本身不区分 token；取消标记经 `QueryContext.cancellationToken` 传入 `SentenceExecutor`

---

### 2b. SentenceExecutor 执行流程（Phase 2 核心）

文件：`ext/src/linq/translator/SentenceExecutor.cs`

```
SentenceExecutor.Execute<TResult>(bag, context, expression, parameters)
  ├─ ExecuteWriteOrAlternative     // DML / InsertOrUpdate
  ├─ ExecuteEnumerable             // IEnumerable<T>
  │    ├─ FinalizeBag              // EntitySelectProjector + SqlOptimizer.Finalize
  │    ├─ BuildSqlBuilder          // SetParameters → ClauseTranslateVisitor.Visit
  │    └─ kit.query<T>().ToList() + NavColumnLoader
  └─ ExecuteScalar                 // Count / 标量
```

**GetSqlText** 路径：`FinalizeBag` → `RunnerContextFactory.Create` → `QueryMate.TranslateCmds` → 拼接 SQL。

**参数绑定**：`QueryMate.SetParameters(bag, expression, ...)` → `ClauseTranslateVisitor.ParameterValues` → `builder.ps`。

---

### 3. QueryMate：从表达式到 SentenceBag

文件：`ext/src/linq/src/linq/query/Query.until.cs`

#### 3.1 GetQuery：从 LINQ Expression 到 SentenceBag

```39:122:ext/src/linq/src/linq/query/Query.until.cs
internal static SentenceBag<T> GetQuery<T>(DBInstance DB, ref Expression expr, out bool dependsOnParameters)
{
    using var mt = ActivityService.Start(ActivityID.GetQueryTotal);

    ExpressionTreeOptimizationContext optimizationContext;
    var queryFlags = QueryFlags.None;
    SentenceBag<T>? query;
    bool useCache;

    using (ActivityService.Start(ActivityID.GetQueryFind))
    {
        using (ActivityService.Start(ActivityID.GetQueryFindExpose))
        {
            optimizationContext = new ExpressionTreeOptimizationContext(DB);

            expr = optimizationContext.AggregateExpression(expr);

            dependsOnParameters = false;
        }

        var Opti = DB.dialect.Option;

        //useCache = !Opti.DisableQueryCache;

        // ... 省略：按表达式找缓存 Query 的逻辑 ...

        // 公开表达式（ExposeExpression）
        var exposed = ExpressionBuilder.ExposeExpression(expr, DB, optimizationContext, null,
            optimizeConditions: true, compactBinary: false);

        var isExposed = !ReferenceEquals(exposed, expr);
        expr = exposed;

        // ... 省略：Expose 后再次查缓存的逻辑 ...
    }

    using (var mc = ActivityService.Start(ActivityID.GetQueryCreate))
        query = CreateQuery<T>(optimizationContext, new ParametersContext(expr, null, optimizationContext, DB),
            DB, expr,null,null);

    return query;
}
```

- **职责**：从原始 LINQ `Expression` 生成可执行的 `SentenceBag<T>`。
- **关键步骤**：
  1. 创建 `ExpressionTreeOptimizationContext`：负责表达式树级别的优化（`AggregateExpression` 等）。
  2. 通过 `ExpressionBuilder.ExposeExpression` 对表达式进行“公开 / 展开”：
     - 去除对 `IDataContext`、`ExpressionQueryImpl` 等的直接引用。
     - 展开位于常量中的 Lambda，使执行树结构更“平坦”，便于后续 SQL 构造。
  3. 创建 `ParametersContext`，封装表达式 + 运行时参数环境。
  4. 调用 `CreateQuery<T>` 生成 `SentenceBag<T>`。

#### 3.2 CreateQuery：表达式到 SentenceBag 的最终构建

```131:155:ext/src/linq/src/linq/query/Query.until.cs
internal static SentenceBag<T> CreateQuery<T>(
    ExpressionTreeOptimizationContext optimizationContext,
    ParametersContext parametersContext,
    DBInstance DB,
    Expression expr,
    ParameterExpression[]? compiledParameters,
    object?[]? parameterValues)
{
    SentenceBag<T> query = new SentenceBag<T>();

    try
    {
        query = ClauseCompiler.Build<T>(
            false, optimizationContext, parametersContext, DB, expr, compiledParameters, parameterValues);
        if (query.ErrorExpression != null)
        {
            query = ClauseCompiler.Build<T>(
                true, optimizationContext, parametersContext, DB, expr, compiledParameters, parameterValues);
            if (query.ErrorExpression != null)
                throw new Exception("表达式编译错误！");
        }
    }
    catch (Exception)
    {
        throw;
    }

    return query;
}
```

- **职责**：真正调用 `ExpressionBuilder` 将 LINQ 表达式转换为 `SentenceBag<T>`。
- **相同职责分支**：`ExpressionBuilder` 的两次构建调用。
  - 第一次：`new ExpressionBuilder(false, ...)`：
    - 通常是“正常模式”/“快速模式”，不输出过多错误信息。
  - 第二次（仅当 `ErrorExpression != null` 时）：
    - `new ExpressionBuilder(true, ...)`：
      - 可能为“诊断模式”/“调试模式”，允许收集更详细的错误表达式信息。
  - **分发依据**：第一次构建是否产出 `ErrorExpression`：
    - 否 → 直接返回构建成功的 `SentenceBag`。
    - 是 → 再试一次（true 模式），仍失败则抛出 `"表达式编译错误！"`。

#### 3.3 SetParameters & GetCommand：从 Sentence 到 SQL 命令

```159:187:ext/src/linq/src/linq/query/Query.until.cs
internal static void SetParameters(
    SentenceBag query, Expression expression, DBInstance? parametersContext,
    object?[]? parameters, SentenceItem sentence, SqlParameterValues parameterValues)
{
    var queryContext = sentence;

    foreach (var p in queryContext.ParameterAccessors)
    {
        var providerValue = p.ValueAccessor(expression, parametersContext, parameters);
        DbDataType? dbDataType = null;

        if (providerValue is IEnumerable items && p.ItemAccessor != null)
        {
            var values = new List<object?>();
            foreach (var item in items)
            {
                values.Add(p.ItemAccessor(item));
                dbDataType ??= p.DbDataTypeAccessor(expression, item, parametersContext, parameters);
            }
            providerValue = values;
        }

        dbDataType ??= p.DbDataTypeAccessor(expression, null, parametersContext, parameters);
        parameterValues.AddValue(p.SqlParameter, providerValue, p.SqlParameter.Type.WithSetValues(dbDataType.Value));
    }
}
```

```190:307:ext/src/linq/src/linq/query/Query.until.cs
internal static SentenceCmds GetCommand(DBInstance dataContext, SentenceItem query,
    IReadOnlyParaValues? parameterValues, bool forGetSqlText, int startIndent = 0)
{
    bool aquiredLock = false;
    try
    {
        Monitor.Enter(query, ref aquiredLock);

        var statement = query.Statement;

        if (query.cmds != null)
        {
            return query.cmds;
        }

        var continuousRun = query.IsContinuousRun;

        if (continuousRun)
        {
            Monitor.Exit(query);
            aquiredLock = false;
        }

        var preprocessContext = new EvaluateContext(parameterValues);

        if (!continuousRun)
        {
            if (!statement.IsParameterDependent)
            {
                // 根据 provider 优化参数依赖的逻辑被注释掉
            }
        }

        var cmds = new SentenceCmds();
        cmds.Sql = statement;

        var translator = dataContext.dialect.clauseTranslator;
        translator.Prepare(dataContext);

        var optimizeAndConvertAll = !continuousRun && !statement.IsParameterDependent;

        // ... 传统 SQLBuilder + 优化器通路被注释，改由 translator 直接 Translate ...

        cmds = translator.Translate(statement);

        if (optimizeAndConvertAll)
        {
            query.cmds = cmds;
            query.Aliases = null;
        }

        query.IsContinuousRun = true;
        return cmds;
    }
    finally
    {
        if (aquiredLock)
            Monitor.Exit(query);
    }
}
```

- **职责分工**：
  - `SetParameters`：负责把 LINQ 表达式中的参数（含集合）映射到 SQL 参数值（`SqlParameterValues`），包括：
    - 利用 `ParameterAccessors` 读取运行时值。
    - 处理集合参数（IN 子句场景）。
    - 决定 `DbDataType` 并附加到 `SqlParameter` 上。
  - `GetCommand`：负责把 `SentenceItem`（逻辑 SQL 语句对象）转换为可执行的 `SentenceCmds`：
    - 通过 `dataContext.dialect.clauseTranslator.Translate(statement)` 把中间 SQL 语法树翻译为具体方言命令集。
    - 处理是否可缓存（`optimizeAndConvertAll`，以及 `query.cmds` 的缓存赋值）。
    - 保证线程安全（`Monitor.Enter/Exit`）和多次执行的性能（`IsContinuousRun`）。

- **相同职责分发点**：
  - **是否重用已生成的 `cmds`**：
    - 若 `query.cmds != null`：直接返回缓存的 `SentenceCmds`。
    - 否则：首次构造并视情况缓存。
  - **是否“一次性优化并转换所有查询”**（`optimizeAndConvertAll`）：
    - 分发依据：`!continuousRun && !statement.IsParameterDependent`。
    - 真时允许一次性优化 + 转换并缓存结果。

#### 3.4 TranslateCmds：基于 RunnerContextFactory（Phase 2）

```316:323:ext/src/linq/src/linq/query/Query.until.cs
internal static SentenceCmds TranslateCmds(RunnerContext context, SentenceItem sentence, bool forGetSqlText)
{
    var parameterValues = new SqlParameterValues();
    var bag = context.sentenceBag ?? throw new InvalidOperationException(...);
    var (expression, parameters) = RunnerContextFactory.ResolveExecutionArgs(context);
    SetParameters(bag, expression, context.dataContext, parameters, sentence, parameterValues);
    var cmds = GetCommand(context.dataContext, sentence, parameterValues, forGetSqlText);
    return cmds;
}
```

- **参数来源**：`RunnerContextFactory.ResolveExecutionArgs` — 优先 `context.expression` / `context.paras`，否则 `bag.srcExp`
- **已移除**：~~`bag.finalExp`~~、~~`premble`~~

### 4. SentenceBag / Runner（Phase 2）

文件：`ext/src/linq/src/linq/query/SentenceBag.cs`

**SentenceBag 关键字段：**

| 字段 | 说明 |
|------|------|
| `Sentences` | `SentenceItem` 列表（Statement + ParameterAccessors） |
| `srcExp` | 原始 LINQ 表达式（参数解析唯一来源） |
| `NavColumns` | LoadWith 导航列，执行后 `NavColumnLoader` 二次查询 |
| `IsCacheable` | 无 NavColumns 且单语句时可缓存 |
| `buildContext` | 编译上下文（调试 / 二次投影） |
| `Runner` | 默认 `BasicSentenceRunner`，委托 `SentenceExecutor` |

**已移除：** ~~`finalExp`~~、~~`ExecuteType`~~、~~`Preambles`~~、~~编译期配置 Mapper 的 `whenGetElement`~~

**BasicSentenceRunner 默认行为：**

```29:43:ext/src/linq/src/linq/query/BasicSentenceRunner.cs
static object? DefaultGetElement(RunnerContext context)
{
    var bag = context.sentenceBag ?? throw ...;
    var db = context.dataContext ?? bag.DBLive;
    var (expression, parameters) = RunnerContextFactory.ResolveExecutionArgs(context);
    return SentenceExecutor.ExecuteObject(bag, db, expression, parameters);
}
```

`BasicSentenceRunner<T>.DefaultGetResultEnumerable`：无 NavColumns 时用 `StreamingResultEnumerable`；有 LoadWith 时 `ExecuteList` + 物化。

<details>
<summary>Phase 1 归档：SentenceBag Preambles / InitPreambles（已删除）</summary>

原 `SentenceBag` 含 `_preambles`、`InitPreambles`、`finalExp`、`ExecuteType` 等，执行前跑前置查询并用 `finalExp` 做 DbDataReader 映射。Phase 2 已全部移除。

</details>

---

### 5. RunnerContext / RunnerContextFactory（Phase 2）

```11:20:ext/src/linq/src/linq/query/RunnerContext.cs
internal class RunnerContext
{
    public DBInstance dataContext = default!;
    public Expression? expression;
    public SentenceBag? sentenceBag;
    public object?[]? paras;
    public CancellationToken cancellationToken;
}
```

`RunnerContextFactory.Create` / `ResolveExecutionArgs` 统一解析 `expression` 与 `parameters`，供 `TranslateCmds`、`BasicSentenceRunner`、`ExpressionQuery` 共用。

**已移除：** ~~`premble`~~

---

### 6. Execution 全流程（Phase 2 时序）

1. **`BaseQueryCompiler.Execute<TResult>(query)`** → `EntityVisitCompiler.DoCompile` → 得到 `Func<QueryContext, TResult>`
2. **执行委托** → `SentenceExecutor.Execute<TResult>(bag, ctx, expression)`
3. **`FinalizeBag`** — `EntitySelectProjector` + `SqlOptimizer.Finalize` + `IsValidQuery`
4. **`BuildSqlBuilder` / `TranslateCmds`** — `SetParameters` → `ClauseTranslateVisitor`（注入 `ParameterValues`）
5. **查询** — `SQLBuilder.query<T>()` / `count()` / DML `ExeNonQuery`
6. **LoadWith** — `NavColumnLoader.LoadNavChilds`（若有 `NavColumns`）
7. **返回** `TResult`

**测试：** SQLite 端到端见 `Tests/src/TestExt/LINQTest.useBus1`、`LinqCompileTests.EntityVisit_Where_ExecutesAgainstSqlite`（`LinqSqliteTestFixture`）。

<details>
<summary>Phase 1 归档：InitPreambles + DbDataReader Mapper 时序（已删除）</summary>

原流程在步骤 2 前调用 `InitPreambles`，Runner 内通过 `BuildMapper` + `finalExp` 逐行映射 `DbDataReader`。Phase 2 已替换为 `query<T>()`。

</details>

---

### 7. 各层职责汇总（Phase 2）

| 层 | 职责 |
|----|------|
| **EntityVisitCompiler** | 编译 `SentenceBag`，委托 `SentenceExecutor.Execute` |
| **SentenceExecutor** | Finalize → 翻译 → `SQLBuilder` 执行 → `NavColumnLoader` |
| **QueryMate** | `GetQuery`（编译）、`SetParameters`、`TranslateCmds` |
| **ClauseTranslateVisitor** | Statement → `SQLBuilderClause`；`ParameterValues` 绑定 |
| **Pure SQLBuilder** | `query<T>()` 实体物化（唯一映射路径） |

**已废弃分发：** Preambles 同步/异步、Mapper 泛型/非泛型 Runner 配置、~~`finalExp`~~ 参数源。

---

### 8. 小结

- **Compile**：`QueryMate.GetQuery` → `ClauseCompiler.Compile` → `SentenceBag`
- **Execute**：`SentenceExecutor` → `SQLBuilder.query<T>()` + 可选 `NavColumnLoader`
- **Inspect**：`SentenceExecutor.GetSqlText` / `bag.Sentences[0].Statement.SelectQuery`

集成测试：`Tests/src/TestHelpers/LinqSqliteTestFixture.cs` + `LinqCompileTests` / `LINQTest`。

