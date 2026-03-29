## EntityVisitCompiler 执行过程解析

本文从一次 LINQ 查询调用开始，追踪 `EntityVisitCompiler` 及其关联组件的整个执行链路，说明每个关键类的职责、调用方向，以及在“相同职责”场景下的分发依据与分支责任，直到 SQL 执行并返回最终结果为止。

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

### 2. EntityVisitCompiler：编译实体查询

文件：`ext/src/linq/core/EntityVisitCompiler.cs`

```21:59:ext/src/linq/core/EntityVisitCompiler.cs
internal class EntityVisitCompiler : BaseQueryCompiler
{
    public EntityVisitCompiler(DBInstance DB) : base(DB)
    {
    }

    public override Func<QueryContext, TResult> DoCompile<TResult>(Expression expression, QueryContext context)
    {
        bool depon;
        var query = QueryMate.GetQuery<TResult>(DB, ref expression, out depon);
        object?[]? Parameters = null;
        return (context) =>
        {
            var Preambles = query.InitPreambles(DB, expression, Parameters);
            if (context.cancellationToken != null)
            {
                var AsyRes = query.Runner.loadElementAsync(new RunnerContext
                {
                    dataContext = DB,
                    expression = expression,
                    paras = Parameters,
                    sentenceBag = query,
                    premble = Preambles
                });
                return (TResult)AsyRes.Result;
            }
            else
            {

            }
            var res = query.Runner.loadElement(new RunnerContext
            {
                dataContext = DB,
                expression = expression,
                paras = Parameters,
                sentenceBag = query,
                premble = Preambles
            });
            return (TResult)res;
        };
    }
}
```

#### 2.1 DoCompile 的整体职责

1. 调用 `QueryMate.GetQuery<TResult>`，将 LINQ 表达式编译成 `SentenceBag<TResult>`（SQL 模型包）。
2. 构造并返回一个 `Func<QueryContext, TResult>` 委托，这个委托在执行时：
   - 调用 `SentenceBag.InitPreambles` 初始化前置查询（`Preambles`）。
   - 根据 `QueryContext.cancellationToken` 情况，选择异步 Runner 或同步 Runner 执行：
     - 有 `cancellationToken`：调用 `query.Runner.loadElementAsync`，但通过 `.Result` 同步取回结果（对外接口仍是同步 `TResult`）。
     - 无 `cancellationToken`：调用 `query.Runner.loadElement`。

#### 2.2 相同职责的分发：同步 vs 异步 Runner

- **职责**：执行 SQL 并返回最终 `TResult`。
- **分发依据**：`QueryContext.cancellationToken` 是否非空。
  - `context.cancellationToken != null`：
    - 使用 `ISentenceRunner.loadElementAsync`，允许内部感知取消标记。
    - 最终通过 `Task.Result` 同步等待（外层调用仍为同步签名）。
  - `context.cancellationToken == null`：
    - 使用 `ISentenceRunner.loadElement`。
- **承担职责**：
  - `SentenceBag.Runner`/`SentenceBag<TResult>.Runner`：选择具体的 Runner 实现（缺省是 `BasicSentenceRunner` / `BasicSentenceRunner<T>`）。
  - `RunnerContext`：封装执行所需的全部上下文信息（`DBInstance`、表达式、参数、`SentenceBag`、前置结果等）。

> 注意：此处虽然走了 `loadElementAsync`，但并未使用 `InitPreamblesAsync`，而是仍然同步初始化 `Preambles`，再同步等待异步结果。

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
        query = new ExpressionBuilder(
            false, optimizationContext, parametersContext, DB, expr, compiledParameters, parameterValues
        ).doBuild<T>();
        if (query.ErrorExpression != null)
        {
            query = new ExpressionBuilder(
                true, optimizationContext, parametersContext, DB, expr, compiledParameters, parameterValues
            ).doBuild<T>();
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

#### 3.4 TranslateCmds / GetQueryCmds：基于 RunnerContext 的命令生成

```316:323:ext/src/linq/src/linq/query/Query.until.cs
internal static SentenceCmds TranslateCmds(RunnerContext context,SentenceItem sentence, bool forGetSqlText)
{
    var parameterValues = new SqlParameterValues();
    var bag = context.sentenceBag;
    SetParameters(bag, bag.finalExp, bag.DBLive, context.paras, sentence, parameterValues);
    var cmds = GetCommand(context.dataContext, sentence, parameterValues, forGetSqlText);
    return cmds;
}
```

```325:337:ext/src/linq/src/linq/query/Query.until.cs
internal static SentenceCmds GetQueryCmds(SentenceBag query, DBInstance parametersContext,
    int queryNumber, Expression expression, object?[]? parameters, object?[]? preambles) {
    var context = new RunnerContext
    {
        sentenceBag = query,
        dataContext = parametersContext,
        expression = expression,
        paras = parameters,
        premble = preambles
    };

    var res = TranslateCmds(context, query.Sentences[queryNumber], false);
    return res;
}
```

- **职责**：作为 `Runner` 的“工具方法”，在执行某条 `SentenceItem` 时：
  1. 构造 `RunnerContext`（如在 `GetQueryCmds` 中）。
  2. 根据 `SentenceBag` 和 `RunnerContext` 当前的参数与前置结果，调用 `SetParameters` + `GetCommand`，获得具体方言 SQL 命令。
- **使用场景**：通常由 `Runner` 内部调用，用于在真正执行数据库命令前获取 SQL 文本和参数集合。

---

### 4. SentenceBag / Runner：承载 LINQ 查询及其执行器

文件：`ext/src/linq/src/linq/query/SentenceBag.cs`

```17:61:ext/src/linq/src/linq/query/SentenceBag.cs
internal class SentenceBag
{
    public DBInstance DBLive;
    public List<SentenceItem> Sentences;
    public ExecuteType ExecuteType;
    public Expression srcExp;
    public Type EntityType;
    public Expression ErrorExpression;
    public bool IsFinalized=false;
    public Expression finalExp;
    public IBuildContext buildContext;

    private ISentenceRunner runner;

    public virtual ISentenceRunner Runner {
        get {
            if (runner == null) {
                runner = new BasicSentenceRunner();
            }
            return runner;
        }
    }

    public void add(SentenceItem sentence) {
        if (Sentences is null) {
            Sentences= new List<SentenceItem>();
        }
        this.Sentences.Add(sentence);
    }

    // ... 省略参数化信息 & Preambles 的维护 ...

    Preamble[]? _preambles;

    internal void SetPreambles(List<Preamble>? preambles)
    {
        _preambles = preambles?.ToArray();
    }
    internal bool IsAnyPreambles()
    {
        return _preambles?.Length > 0;
    }
    internal object?[]? InitPreambles(DBInstance dc, Expression rootExpression, object?[]? ps)
    {
        if (_preambles == null)
            return null;

        var preambles = new object[_preambles.Length];
        for (var i = 0; i < preambles.Length; i++)
        {
            preambles[i] = _preambles[i].Execute(new RunnerContext
            {
                dataContext = dc,
                expression = rootExpression,
                paras = ps,
                premble = preambles
            });
        }

        return preambles;
    }

    internal async Task<object?[]?> InitPreamblesAsync(DBInstance dc, Expression rootExpression,
        object?[]? ps, CancellationToken cancellationToken)
    {
        if (_preambles == null)
            return null;

        var preambles = new object[_preambles.Length];
        for (var i = 0; i < preambles.Length; i++)
        {
            preambles[i] = await _preambles[i].ExecuteAsync(new RunnerContext
            {
                dataContext = dc,
                expression = rootExpression,
                paras = ps,
                premble = preambles,
                cancellationToken = cancellationToken
            }).ConfigureAwait(mooSQL.linq.Common.Configuration.ContinueOnCapturedContext);
        }

        return preambles;
    }
}
```

```144:160:ext/src/linq/src/linq/query/SentenceBag.cs
internal class SentenceBag<T>:SentenceBag
{
    private ISentenceRunner<T> runner;

    public ISentenceRunner<T> Runner
    {
        get {
            if (runner == null)
            {
                runner = new BasicSentenceRunner<T>();
            }
            return runner;
        }
    }
}
```

#### 4.1 SentenceBag 的职责

- 代表一次 LINQ 查询的**完整 SQL 模型包**，包含：
  - `Sentences`：一个或多个 SQL “语句单元”集合。
  - `ExecuteType`：执行类型（单值、列表、分页等）。
  - 原始表达式 `srcExp` 与最终表达式 `finalExp`。
  - 构建上下文 `IBuildContext`，用于表达式到 SQL 结构的中间状态。
  - 前置执行单元 `Preambles` 的定义及初始化方法。
- **相同职责分发**：
  - `SentenceBag` vs `SentenceBag<T>`：
    - 分发依据：是否需要在编译期绑定泛型结果类型 `T`。
    - 职责差异：前者只暴露非泛型 `ISentenceRunner`，后者暴露强类型 `ISentenceRunner<T>`。

#### 4.2 Runner 的默认实现：BasicSentenceRunner

文件：`ext/src/linq/src/linq/query/BasicSentenceRunner.cs`

```10:33:ext/src/linq/src/linq/query/BasicSentenceRunner.cs
internal class BasicSentenceRunner:ISentenceRunner
{
    internal Func<RunnerContext, object?> GetElement = null!;
    internal Func<RunnerContext, Task<object?>> GetElementAsync = null!;

    public void whenGetElement(Func<RunnerContext, object?> GetElement)
    {
        this.GetElement = GetElement;
    }

    public void whenGetElementAsync(Func<RunnerContext, Task<object?>> GetElementAsync)
    {
        this.GetElementAsync = GetElementAsync;
    }

    public object? loadElement(RunnerContext context)
    {
        return this.GetElement(context);
    }

    public Task<object?> loadElementAsync(RunnerContext context)
    {
        return this.GetElementAsync(context);
    }
}
```

```36:49:ext/src/linq/src/linq/query/BasicSentenceRunner.cs
internal class BasicSentenceRunner<T> : BasicSentenceRunner , ISentenceRunner<T>
{
    protected Func<RunnerContext, IResultEnumerable<T>> GetResultEnumerable = null!;

    public IResultEnumerable<T> loadResultList(RunnerContext context)
    {
        return GetResultEnumerable(context);
    }

    public void whenGetResultEnumerable(Func<RunnerContext, IResultEnumerable<T>> GetResultEnumerable)
    {
        this.GetResultEnumerable = GetResultEnumerable;
    }
}
```

- **职责**：
  - `BasicSentenceRunner` / `BasicSentenceRunner<T>` 是可配置的执行器骨架：
    - 通过 `whenGetElement` / `whenGetElementAsync` 注入真正的执行逻辑。
    - 对外提供统一的 `loadElement` / `loadElementAsync` / `loadResultList` 接口。
- **相同职责分发**：
  - 同步执行 vs 异步执行：
    - 分发依据：调用的是 `loadElement` 还是 `loadElementAsync`。
    - 各自职责：分别负责同步 / 异步的查询执行，但底层由注入的委托（通常由构建阶段配置）实现具体逻辑。
  - 非泛型 vs 泛型：
    - 分发依据：是否需要枚举 `T` 形态的结果集。
    - `BasicSentenceRunner`：返回 `object?`，可用于标量或非泛型场景。
    - `BasicSentenceRunner<T>`：通过 `IResultEnumerable<T>` 支持强类型枚举。

---

### 5. RunnerContext：执行阶段的统一上下文

文件：`ext/src/linq/src/linq/query/RunnerContext.cs`

```11:24:ext/src/linq/src/linq/query/RunnerContext.cs
internal class RunnerContext
{
    public DBInstance dataContext;
    public Expression expression;

    public SentenceBag sentenceBag;

    public object?[]? paras;
    public object?[]? premble;
    public CancellationToken cancellationToken;
}
```

- **职责**：在执行某个 Sentence / Query 时，统一携带所需的所有环境：
  - 数据库上下文：`dataContext`（即 `DBInstance`）。
  - 原始表达式：`expression`。
  - SQL 模型包：`sentenceBag`。
  - 参数：`paras`。
  - 前置查询（Preambles）执行结果：`premble`。
  - 取消标记：`cancellationToken`（异步执行时使用）。
- **相同职责分发**：
  - 同一套上下文结构服务于多种执行函数：
    - `Preamble.Execute` / `ExecuteAsync`。
    - `BasicSentenceRunner.loadElement` / `loadElementAsync`。
    - `QueryMate.TranslateCmds`（用于生成 SQL 命令）。
  - 分发依据是“调用方的功能”，`RunnerContext` 本身不做分支，而是提供统一数据源。

---

### 6. Execution 全流程串联（时序视角）

以 `BaseQueryCompiler.Execute<TResult>(Expression query)` 为起点，完整流程如下：

1. **调用编译器执行**  
   - 入口：`BaseQueryCompiler.Execute<TResult>(Expression query)`。
   - 步骤：
     1. 构建 `QueryContext`（绑定 `DBInstance`）。
     2. 调用 `PrepareExpression`（当前直接返回原表达式）。
     3. 调用 `EntityVisitCompiler.DoCompile<TResult>(expressionNext, context)` 获取委托 `Func<QueryContext, TResult>`。
     4. 执行该委托：`fun(context)`。

2. **EntityVisitCompiler.DoCompile**  
   - 通过 `QueryMate.GetQuery<TResult>(DB, ref expression, out depon)`：
     - 优化 & 公开 LINQ 表达式。
     - 使用 `ExpressionBuilder` 构建 `SentenceBag<TResult>`。
   - 返回的委托在执行时：
     1. 调用 `query.InitPreambles(DB, expression, Parameters)`，根据 `SentenceBag` 中的 `_preambles` 初始化所有前置查询。
     2. 根据 `QueryContext.cancellationToken` 分支：
        - 有 token：`query.Runner.loadElementAsync(new RunnerContext { ... }).Result`。
        - 无 token：`query.Runner.loadElement(new RunnerContext { ... })`。

3. **SentenceBag.Runner（BasicSentenceRunner）阶段**  
   - 根据执行类型（标量 / 列表 / 分页等），在构建 `SentenceBag` 过程中已为 `Runner` 配置好：
     - `whenGetElement` / `whenGetElementAsync` 委托。
     - 或针对 `T` 类型结果的 `whenGetResultEnumerable`。
   - `loadElement` / `loadElementAsync` 内部：
     - 使用 `RunnerContext` 中的 `sentenceBag` 和 `dataContext` 等信息。
     - 调用 `QueryMate.TranslateCmds` / `GetQueryCmds`，生成 `SentenceCmds`（可执行 SQL 命令集合）。
     - 调用底层 ADO 执行 SQL（`DBInstance` + `dialect` 等），映射结果到 C# 对象 / 集合 / 标量。

4. **QueryMate.TranslateCmds / GetQueryCmds**  
   - 通过 `SetParameters`：
     - 根据 `SentenceItem.ParameterAccessors` 读取运行时参数。
     - 填充 `SqlParameterValues`。
   - 通过 `GetCommand`：
     - 借助 `dataContext.dialect.clauseTranslator` 将中间 SQL 语法树翻译为具体方言命令。
     - 处理缓存、参数依赖、并发执行等。

5. **结果返回**  
   - `BasicSentenceRunner` 将 ADO 执行结果组装为：
     - 单值 `TResult`。
     - 或 `IResultEnumerable<T>` → 列表 / 分页结果。
   - `EntityVisitCompiler` 的委托返回 `TResult` 给 `BaseQueryCompiler.Execute` / `ExecuteAsync`。
   - 调用方得到最终查询结果。

---

### 7. 各层职责与分发依据汇总

#### 7.1 按层划分职责

- **编译器层（BaseQueryCompiler / EntityVisitCompiler）**
  - 决定同步/异步表象及 `CancellationToken` 传递。
  - 调用 `QueryMate` 将 LINQ 表达式编译为可执行 `SentenceBag`。
  - 构造顶层执行委托，注入 `RunnerContext` 并选择 Runner 的同步或异步通路。

- **查询构建层（QueryMate + ExpressionBuilder + SentenceBag）**
  - 负责：
    - 表达式树优化 & 公开（`ExpressionBuilder.ExposeExpression`）。
    - 将表达式翻译为中间 SQL 模型（`SentenceBag`/`SentenceItem`）。
    - 维护前置执行单元（`Preambles`）及其初始化方法。

- **执行器层（SentenceBag.Runner + BasicSentenceRunner + QueryMate.TranslateCmds）**
  - 在具体执行阶段根据 `RunnerContext`：
    - 生成参数 & SQL 命令（`SetParameters`，`GetCommand`，`TranslateCmds`）。
    - 调用 `DBInstance` & `dialect.clauseTranslator` 执行实际 SQL。
    - 将数据行映射回实体/匿名对象/标量。

#### 7.2 典型“相同职责分发分支”与依据

1. **同步 vs 异步执行**  
   - 分发点：
     - `BaseQueryCompiler.Execute` vs `ExecuteAsync`。
     - `EntityVisitCompiler` 中 `loadElement` vs `loadElementAsync` 调用。
   - 分发依据：
     - 是否提供 `CancellationToken`（外层）。
     - 调用方是需要 `Task` 风格还是同步结果（内层）。
   - 职责差异：
     - 异步通路允许底层 IO 异步 & 响应取消。
     - 同步通路阻塞直到结果返回。

2. **普通模式 vs 调试模式 ExpressionBuilder**  
   - 分发点：`CreateQuery<T>` 中的两次 `new ExpressionBuilder(...).doBuild<T>()`。
   - 分发依据：第一次构建后 `query.ErrorExpression` 是否非空。
   - 职责差异：
     - 普通模式：偏向性能。
     - 调试模式：偏向错误诊断，若仍失败则抛异常。

3. **缓存命令 vs 即时生成命令**  
   - 分发点：`GetCommand` 中 `if (query.cmds != null)` 与 `optimizeAndConvertAll` 逻辑。
   - 分发依据：
     - `query.cmds` 是否已有缓存。
     - 是否适合一次性优化并缓存（`!continuousRun && !statement.IsParameterDependent`）。
   - 职责差异：
     - 缓存命令：提高多次相同查询的执行性能。
     - 即时命令：保证参数依赖查询的正确性。

4. **泛型 Runner vs 非泛型 Runner**
   - 分发点：`SentenceBag` vs `SentenceBag<T>` 的 `Runner` 属性。
   - 分发依据：是否需要强类型 `T` 的结果集合。
   - 职责差异：
     - 非泛型 Runner：返回 `object?`，适用于通用/标量场景。
     - 泛型 Runner：提供 `loadResultList`，便于逐行映射为 `T` 并枚举。

5. **前置执行同步 vs 异步**
   - 分发点：`InitPreambles` vs `InitPreamblesAsync`。
   - 分发依据：是否需要异步前置处理以及是否有 `CancellationToken`。
   - 职责差异：
     - 同步前置：用于同步查询。
     - 异步前置：允许耗时较长的准备过程非阻塞执行。

---

### 8. 小结

- `EntityVisitCompiler` 自身代码不多，但站在整个 LINQ → SQL 执行链路的**关键连接点**：
  - 向下衔接 `QueryMate` / `ExpressionBuilder` 的表达式解析与 SQL 模型构建。
  - 向上提供 `Func<QueryContext, TResult>`，统一封装同步/异步 Runner 的调用与 `RunnerContext` 构建。
- 真正“厚重”的逻辑分布在：
  - `QueryMate`（表达式优化 / 公开 / 构建 `SentenceBag` / 参数与 SQL 命令生成）。
  - `SentenceBag` + `Runner` 系列（承载 SQL 模型与执行策略）。
  - `dialect.clauseTranslator`（将中间 SQL 语法树翻译为具体数据库方言 SQL）。
- 通过本文可以从宏观上把握一次 LINQ 查询在 mooSQL 中的行程：  
  **LINQ Expression → ExpressionBuilder / SentenceBag → RunnerContext / Runner → SqlTranslator / ADO 执行 → C# 结果对象**。

