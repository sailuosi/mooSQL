## ExpressionBuilder 构建 SentenceBag\<TResult\> 过程解析

本文专门解释 `ExpressionBuilder` 在 `QueryMate.CreateQuery<T>` 中**如何把 LINQ 表达式构建为 `SentenceBag<T>`**，包括内部步骤、关键类、以及各个分支的职责划分。建议结合 `EntityVisitCompiler-执行过程解析.md` 一起阅读。

---

### 1. 入口回顾：QueryMate.CreateQuery → ExpressionBuilder.doBuild

`QueryMate.CreateQuery<T>` 中的核心调用：

```131:147:ext/src/linq/src/linq/query/Query.until.cs
internal static SentenceBag<T> CreateQuery<T>(
    ExpressionTreeOptimizationContext optimizationContext,
    ParametersContext parametersContext,
    DBInstance DB,
    Expression expr,
    ParameterExpression[]? compiledParameters,
    object?[]? parameterValues)
{
    SentenceBag<T> query =new SentenceBag<T>();

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

- **职责**：封装对 `ExpressionBuilder` 的使用，得到最终的 `SentenceBag<T>`。
- **关键点**：
  - `validateSubqueries` 两种模式：
    - `false`：正常构建。
    - `true`：如果第一次构建出现 `ErrorExpression`，以“更严格/诊断模式”再构建一次。
  - 构建的核心逻辑全部在 `ExpressionBuilder.doBuild<T>` 内完成。

---

### 2. ExpressionBuilder 的构造与基础成员

文件：`ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.cs`

```74:100:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.cs
internal sealed partial class ExpressionBuilder : IExpressionEvaluator
{
    bool                                       _validateSubqueries;
    readonly ExpressionTreeOptimizationContext _optimizationContext;
    readonly ParametersContext                 _parametersContext;

    public ExpressionTreeOptimizationContext   OptimizationContext => _optimizationContext;
    public ParametersContext                   ParametersContext   => _parametersContext;

    public ExpressionBuilder(
        bool                              validateSubqueries,
        ExpressionTreeOptimizationContext optimizationContext,
        ParametersContext                 parametersContext,
        DBInstance                        DB,
        Expression                        expression,
        ParameterExpression[]?            compiledParameters,
        object?[]?                        parameterValues)
    {
        _validateSubqueries  = validateSubqueries;

        CompiledParameters = compiledParameters;
        ParameterValues    = parameterValues;
        DBLive             = DB;

        OriginalExpression = expression;

        _optimizationContext = optimizationContext;
        _parametersContext   = parametersContext;
        Expression           = expression;
    }

    public DBInstance DBLive {  get; set; }
    public readonly Expression             OriginalExpression;
    public readonly Expression             Expression;
    public readonly ParameterExpression[]? CompiledParameters;
    public readonly object?[]?             ParameterValues;
}
```

- **职责**：
  - 记录本次构建所需的全部上下文：
    - 已优化的表达式树上下文：`ExpressionTreeOptimizationContext`。
    - 参数收集与访问上下文：`ParametersContext`。
    - 当前数据库环境：`DBLive`（`DBInstance`）。
    - 原始/当前表达式：`OriginalExpression`、`Expression`。
    - 预编译参数及其值：`CompiledParameters`、`ParameterValues`。
  - `validateSubqueries` 标记会影响某些内部检查（例如子查询验证），在本文件中主要作为模式标志存在。

---

### 3. 核心入口：doBuild<T> —— 第一次构建 SentenceBag<T>

```124:177:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.cs
public SentenceBag<T> doBuild<T>()
{
    var res= new SentenceBag<T>();
    res.EntityType = typeof(T);
    using var m = ActivityService.Start(ActivityID.Build);

    var sequence = BuildSequence(new BuildInfo((IBuildContext?)null, Expression, new SelectQueryClause()));

    using var mq = ActivityService.Start(ActivityID.BuildQuery);
    // 读入到结果集

    var statement = sequence.GetResultStatement();

    var sentence = new SentenceItem()
    {
        Statement = statement,
        ParameterAccessors = _parametersContext.CurrentSqlParameters
    };
    res.add(sentence);

    // 第2编译阶段
    // 注意，此处实体类类型暂未传入

    var param = Expression.Parameter(typeof(SentenceBag), "info");

    List<Preamble>? preambles = null;
    BuildQuery<T>(res, sequence, param, ref preambles, new Expression[] { });

    if (res.ErrorExpression == null)
    {
        foreach (var q in res.Sentences)
        {
            if (Tag?.Lines.Count > 0)
            {
                (q.Statement.Tag ??= new()).Lines.AddRange(Tag.Lines);
            }

            if (SqlQueryExtensions != null)
            {
                (q.Statement.SqlQueryExtensions ??= new()).AddRange(SqlQueryExtensions);
            }
        }

        res.SetPreambles(preambles);
        res.SetParameterized(_parametersContext.GetParameterized());
    }

    return res;
}
```

从上往下可以分为三个阶段：

1. **阶段一：构建序列（BuildSequence）**
2. **阶段二：生成 Statement + SentenceItem 并加入 SentenceBag**
3. **阶段三：第二阶段编译（BuildQuery）—— 投影/映射/校验/最终化 + Preambles 收集**

下面分别展开。

---

### 4. 阶段一：BuildSequence —— 从 LINQ 表达式到 IBuildContext

```323:336:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.cs
public IBuildContext BuildSequence(BuildInfo buildInfo)
{
    var buildResult = TryBuildSequence(buildInfo);
    if (buildResult.BuildContext == null)
    {
        var errorExpr = buildResult.ErrorExpression ?? buildInfo.Expression;

        if (errorExpr is SqlErrorExpression error)
            throw error.CreateException();

        throw SqlErrorExpression.CreateException(errorExpr, buildResult.AdditionalDetails);
    }
    return buildResult.BuildContext;
}
```

```285:315:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.cs
public BuildSequenceResult TryBuildSequence(BuildInfo buildInfo)
{
    using var m = ActivityService.Start(ActivityID.BuildSequence);

    var originalExpression = buildInfo.Expression;

    var expanded = ExpandToRoot(buildInfo.Expression, buildInfo);

    if (!ReferenceEquals(expanded, originalExpression))
        buildInfo = new BuildInfo(buildInfo, expanded);

    if (!TryFindBuilder(buildInfo, out var builder))
        return BuildSequenceResult.NotSupported();

    using var mb = ActivityService.Start(ActivityID.BuildSequenceBuild);

    var result = builder.BuildSequence(this, buildInfo);

    if (result.BuildContext != null)
    {
#if DEBUG
        if (!buildInfo.IsTest)
            QueryHelper.DebugCheckNesting(result.BuildContext.GetResultStatement(), buildInfo.IsSubQuery);
#endif
        RegisterSequenceExpression(result.BuildContext, originalExpression);
    }

    if (!result.IsSequence)
        return BuildSequenceResult.Error(originalExpression);

    return result;
}
```

#### 4.1 BuildSequence 的职责

1. 把 `(parentContext = null, 当前Expression, 新建SelectQueryClause)` 封装成 `BuildInfo`。
2. 通过 `TryBuildSequence`：
   - 调用 `ExpandToRoot`：将表达式“展开”到查询根部（处理嵌套 `Queryable` 调用、聚合根等）。
   - 使用 `TryFindBuilder` 根据扩展后的表达式选择**具体的 `ISequenceBuilder` 实现**（例如处理 `Where` / `Select` / `Join` 等不同 LINQ 操作的 Builder）。
   - 调度该 Builder 的 `BuildSequence(this, buildInfo)`，返回 `BuildSequenceResult`。
   - 校验 `BuildContext` 是否有效序列（`IsSequence`），并注册表达式与 `IBuildContext` 之间的对应关系（用于后续 Eager Loading / 关联等）。
3. 返回构建好的 `IBuildContext`，其中已经包含：
   - 代表本次查询的 SQL 结构 `SelectQueryClause`。
   - 当前上下文所处的 From/Join/Where/GroupBy 等信息。

#### 4.2 ISequenceBuilder 的“相同职责分发”

- **职责**：针对不同形态的 LINQ 表达式（`Where`、`Select`、`Join`、`OrderBy`、`GroupBy` 等），生成对应的 `IBuildContext`。
- **分发依据**：表达式树的结构，如：
  - 是否为 `MethodCallExpression` 且是 `Queryable.Where` / `Queryable.Select` 等。
  - 是否为 `ConstantExpression` / `MemberExpression` 等作为查询源。
- **承担职责**：
  - 注入相应的 SQL 片段（Where 条件、Select 列、Join 条件等）到 `SelectQueryClause` 中。
  - 为后续的 Projection/Mapping 提供上下文信息（如当前元素类型、表信息等）。

> 在 `ExpressionBuilder` 里，通过 `FindBuilderImpl`（源生成的 partial 方法）完成“表达式 → 具体 Builder 实现”的选择，这就是这一阶段的“多态分发核心”。

---

### 5. 阶段二：生成 Statement + SentenceItem 并放入 SentenceBag

在 `doBuild<T>` 中，获取到 `sequence`（`IBuildContext`）后：

```130:142:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.cs
var sequence = BuildSequence(new BuildInfo((IBuildContext?)null, Expression, new SelectQueryClause()));

// ...

var statement = sequence.GetResultStatement();

var sentence = new SentenceItem()
{
    Statement = statement,
    ParameterAccessors = _parametersContext.CurrentSqlParameters
};
res.add(sentence);
```

- **`sequence.GetResultStatement()`**：
  - 从 `IBuildContext` 中提取完整的 SQL 语法树 `SelectQueryClause` 或更上层的 `Statement` 对象。
  - 这里已涵盖了当前查询所有结构：SELECT / FROM / JOIN / WHERE / GROUP BY / ORDER BY 等。
- **`SentenceItem` 的职责**：
  - `Statement`：SQL 语句的逻辑结构树。
  - `ParameterAccessors`：在前面构建过程中由 `ParametersContext` 收集的 SQL 参数访问器。
- **`SentenceBag<T>.add(sentence)`**：
  - 把本次查询的“主语句单元”加入 `SentenceBag` 的 `Sentences` 集合。
  - 一个 `SentenceBag` 可能包含多个 `SentenceItem`（如批量语句、多语句查询），此处先加入主语句。

---

### 6. 阶段三：BuildQuery<T> —— 投影 / 校验 / Finalize / Preambles

```179:219:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.cs
bool BuildQuery<T>(
    SentenceBag<T> query,
    IBuildContext sequence,
    ParameterExpression queryParameter,
    ref List<Preamble>? preambles,
    Expression[] previousKeys)
{
    var expr = MakeExpression(sequence, new ContextRefExpression(query.EntityType, sequence), ProjectFlags.Expression);

    var finalized = FinalizeProjection(sequence, expr, queryParameter, ref preambles, previousKeys);
    query.finalExp = expr;
    var error = SequenceHelper.FindError(finalized);
    if (error != null)
    {
        query.ErrorExpression = error;
        return false;
    }

    using (ActivityService.Start(ActivityID.FinalizeQuery))
    {
        ISqlOptimizer SqlOptimizer = null;//DataContext.GetSqlOptimizer(DataContext.Options);
        foreach (var sentence in query.Sentences)
        {
            //直接使用上下文的环境参数，不再放入 Query对象
            sentence.Statement = SqlOptimizer.Finalize(DBLive , sentence.Statement);

            if (sentence.Statement.SelectQuery != null)
            {
                if (!SqlProviderHelper.IsValidQuery(sentence.Statement.SelectQuery, null, null, false, DBLive.dialect.Option.ProviderFlags, out var errorMessage))
                {
                    query.ErrorExpression = new SqlErrorExpression(Expression, errorMessage, Expression.Type);
                    return false;
                }
            }
        }

        query.IsFinalized = true;
    }
    // 设置执行动作
    sequence.SetRunQuery<T>(query, finalized);
    return true;
}
```

#### 6.1 MakeExpression：从上下文到结果投影表达式

- `MakeExpression(sequence, new ContextRefExpression(query.EntityType, sequence), ProjectFlags.Expression)`：
  - 以 `ContextRefExpression`（指向当前 `IBuildContext`）为起点，生成最终结果的投影表达式 `expr`：
    - 对应用户 LINQ 中的 `Select` 投影。
    - 若是 `Select(x)`，则是整实体；若是 `Select(new { x.Id, x.Name })`，则是匿名对象构造表达式；若是 `Count` / `First` 等则相应不同。
  - `ProjectFlags.Expression` 表示当前目标是“生成可执行的 .NET 表达式”，而非直接生成 SQL 片段。

#### 6.2 FinalizeProjection：完成构造、急切加载、列绑定

文件：`ExpressionBuilder.QueryBuilder.cs`

```27:56:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.QueryBuilder.cs
Expression FinalizeProjection(
    IBuildContext context,
    Expression expression,
    ParameterExpression queryParameter,
    ref List<Preamble>? preambles,
    Expression[] previousKeys)
{
    // 非查询 表达式 快速返回
    if (expression.NodeType == ExpressionType.Default)
        return expression;

    // 转换所有遗漏的引用
    var postProcessed = FinalizeConstructors(context, expression, true);

    // 处理急切加载查询
    var correctedEager = CompleteEagerLoadingExpressions(postProcessed, context, queryParameter, ref preambles, previousKeys);

    if (SequenceHelper.HasError(correctedEager))
        return correctedEager;

    if (!ExpressionEqualityComparer.Instance.Equals(correctedEager, postProcessed))
    {
        // convert all missed references
        postProcessed = FinalizeConstructors(context, correctedEager, false);
    }

    var withColumns = ToColumns(context, postProcessed);
    return withColumns;
}
```

整体步骤：

1. **FinalizeConstructors**（第一次）：
   - 使用 `_finalizeVisitor` + `ExpressionGenerator`：
     - 展开 `SqlGenericConstructorExpression`（实体构造表达式）。
     - 生成真实的对象初始化表达式树。
2. **CompleteEagerLoadingExpressions**：
   - 处理 `LoadWith` / 关联导航等“急切加载”逻辑。
   - 在结果表达式中插入额外查询或联接，以保证相关导航属性一次性加载到位。
   - 同时收集需要在执行前跑的 “Preambles”（前置查询），通过 `preambles` 输出。
3. **错误检查**：
   - 若 `correctedEager` 包含 `SqlErrorExpression`，直接返回错误表达式（由上一层 `SequenceHelper.FindError` 处理）。
4. **FinalizeConstructors**（必要时第二次）：
   - 如果急切加载修正（`correctedEager`）改变了表达式树，再次跑一遍构造器终结，以确保所有引用被正确转换。
5. **ToColumns**：
   - 遍历表达式树，将 `SqlPlaceholderExpression` 转换为“列绑定”形式：
     - 为每个 SQL 列创建稳定的列引用位置（索引、别名）。
     - 处理嵌套子查询/CTE 的列上推、父子查询列映射等。

> 最终 `FinalizeProjection` 返回的 `withColumns` 就是一个“可用于生成 Reader 映射的投影表达式”：其中的每个字段都对应一个具体的 SQL 列位置信息。

#### 6.3 错误检测与 Finalize（SqlOptimizer + 有效性校验）

```188:216:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.cs
var error = SequenceHelper.FindError(finalized);
if (error != null)
{
    query.ErrorExpression = error;
    return false;
}

using (ActivityService.Start(ActivityID.FinalizeQuery))
{
    ISqlOptimizer SqlOptimizer = null;//DataContext.GetSqlOptimizer(DataContext.Options);
    foreach (var sentence in query.Sentences)
    {
        //直接使用上下文的环境参数，不再放入 Query对象
        sentence.Statement = SqlOptimizer.Finalize(DBLive , sentence.Statement);

        if (sentence.Statement.SelectQuery != null)
        {
            if (!SqlProviderHelper.IsValidQuery(sentence.Statement.SelectQuery, null, null, false, DBLive.dialect.Option.ProviderFlags, out var errorMessage))
            {
                query.ErrorExpression = new SqlErrorExpression(Expression, errorMessage, Expression.Type);
                return false;
            }
        }
    }

    query.IsFinalized = true;
}
```

- **`SequenceHelper.FindError(finalized)`**：
  - 在投影表达式中查找 `SqlErrorExpression`，一旦发现则记录到 `SentenceBag.ErrorExpression`。
- **`SqlOptimizer.Finalize`**：
  - 这里暂时是 `ISqlOptimizer SqlOptimizer = null;` 的占位，说明原版逻辑是：
    - 根据 `DBLive` 获取 SQL 优化器，对 `statement` 进行优化（如常量折叠、Join 简化、Null 处理等）。
- **`SqlProviderHelper.IsValidQuery`**：
  - 按当前数据库方言的规则检测 `SelectQuery` 是否是一个合法的查询：
    - 列/表/别名是否合法。
    - 聚合、分组是否一致。
    - 方言限制（如某些 DB 不支持某类语法）。
  - 若不合法，则生成相应的 `SqlErrorExpression`。

#### 6.4 SetRunQuery：把“如何执行”绑定回 IBuildContext

```217:219:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.cs
// 设置执行动作
sequence.SetRunQuery<T>(query, finalized);
return true;
```

- **职责**：
  - 把 `SentenceBag<T>` 和 `finalized` 投影表达式交给 `IBuildContext` 去设置具体的“执行行为”：
    - 配置 `SentenceBag.Runner` / `SentenceBag<T>.Runner` 的 `whenGetElement` / `whenGetElementAsync` / `whenGetResultEnumerable`。
    - 配置如何根据 `SentenceCmds` + `DbDataReader` 与 `finalized` 投影表达式，将一行数据映射为 `T`。
- 这一步是 **“把静态 SQL 模型（Sentence）和投影表达式绑定成可执行查询（Runner）”** 的关键。

---

### 7. 构建过程中其它关键工具点

#### 7.1 ExposeExpression / ConvertExpressionTree

```362:367:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.cs
public Expression ConvertExpressionTree(Expression expression)
{
    var expr = ExposeExpression(expression, DBLive, _optimizationContext, ParameterValues, optimizeConditions:false, compactBinary:true);

    return expr;
}
```

- **职责**：
  - 利用 `ExposeExpressionVisitor` 对表达式进行：
    - 常量中的 Lambda 展开。
    - 去掉对数据上下文等运行时对象的直接引用，转为可缓存的表达式结构。
    - 按需要优化布尔条件和二元表达式结构（`optimizeConditions` / `compactBinary`）。
- 在 `QueryMate.GetQuery` 里已经调用过 `ExpressionBuilder.ExposeExpression`；`ConvertExpressionTree` 为内部转换提供统一入口。

#### 7.2 BuildFullEntityExpression / EntityConstructor

来自 `ExpressionBuilder.Generation.cs`：

```56:63:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.Generation.cs
public SqlGenericConstructorExpression BuildFullEntityExpression(DBInstance mappingSchema, Expression refExpression, Type entityType, ProjectFlags flags, FullEntityPurpose purpose = FullEntityPurpose.Default)
{
    _entityConstructor ??= new EntityConstructor(this);

    var generic = _entityConstructor.BuildFullEntityExpression( mappingSchema, refExpression, entityType, flags, purpose);

    return generic;
}
```

- **职责**：
  - 构建一个“完整实体构造表达式”（`SqlGenericConstructorExpression`），代表如何将结果行映射到实体的所有字段，包括关联导航。
  - 再由 `FinalizeConstructors` + `ExpressionGenerator` 将其转为真正的 C# 对象初始化代码。
- 这是构建 `SentenceBag<T>` 时，**实体级映射**的基础设施。

#### 7.3 ToReadExpression / BuildMapper：从列到实体的最终映射

来自 `ExpressionBuilder.SqlBuilder.cs`：

```614:717:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.SqlBuilder.cs
public Expression ToReadExpression(
    ExpressionGenerator expressionGenerator,
    NullabilityContext  nullability,
    Expression          expression)
{
    // ... 省略前置简化 ...

    var toRead = simplified.Transform(e =>
    {
        if (e is SqlPlaceholderExpression placeholder)
        {
            if (placeholder.Sql == null)
                throw new InvalidOperationException();
            if (placeholder.Index == null)
                throw new InvalidOperationException();
            //字段描述信息
            var columnDescriptor = QueryHelper.GetColumnDescriptor(placeholder.Sql);
            //字段值类型
            var valueType = columnDescriptor?.DbType.SystemType
                            ?? placeholder.Type;
            //可空
            var canBeNull = nullability.CanBeNull(placeholder.Sql) || placeholder.Type.IsNullable();

            if (canBeNull && valueType != placeholder.Type && valueType.IsValueType && !valueType.IsNullable())
            {
                valueType = valueType.WrapNullable();
            }
            // 可空的包裹类型
            if (placeholder.Type != valueType && valueType.IsNullable() && placeholder.Type == valueType.UnwrapNullable())
            {
                // let ConvertFromDataReaderExpression handle default value
                valueType = placeholder.Type;
            }

            var readerExpression = (Expression)new ConvertFromDataReaderExpression(valueType, placeholder.Index.Value,
                null, DataReaderParam, canBeNull);

            if (placeholder.Type != readerExpression.Type)
            {
                readerExpression = Expression.Convert(readerExpression, placeholder.Type);
            }

            return new TransformInfo(readerExpression);
        }

        // ... 处理 IsNull / RowCounter 等 ...

        return new TransformInfo(e);
    });

    return toRead;
}
```

```725:750:ext/src/linq/src/linq/builder/expressionBuilder/ExpressionBuilder.SqlBuilder.cs
public Expression<Func<IQueryRunner,DBInstance,DbDataReader,Expression,object?[]?,object?[]?,T>> BuildMapper<T>(SelectQueryClause query, Expression expr)
{
    var type = typeof(T);

    if (expr.Type != type)
        expr = Expression.Convert(expr, type);

    var expressionGenerator = new ExpressionGenerator();

    // variable accessed dynamically
    _ = expressionGenerator.AssignToVariable(DataReaderParam, "ldr");

    var readExpr = ToReadExpression(expressionGenerator, new NullabilityContext(query), expr);
    expressionGenerator.AddExpression(readExpr);

    var mappingBody = expressionGenerator.Build();

    var mapper = Expression.Lambda<Func<IQueryRunner, DBInstance, DbDataReader,Expression,object?[]?,object?[]?,T>>(mappingBody,
        QueryRunnerParam,
        ExpressionConstants.DataContextParam,
        DataReaderParam,
        ExpressionParam,
        ParametersParam,
        PreambleParam);

    return mapper;
}
```

- **职责**：
  - 将已经“绑定列信息”的投影表达式，转换为基于 `DbDataReader` 的读取表达式：
    - 对每个 `SqlPlaceholderExpression` 替换为 `ConvertFromDataReaderExpression`，即 “从某列索引读取并转换为目标类型”的表达式。
  - 最终打包为一个 Lambda：  
    `Func<IQueryRunner, DBInstance, DbDataReader, Expression, object?[]?, object?[]?, T>`
    - 后续在 `Runner` 里会被编译并缓存，用来逐行从 Reader 生成类型为 `T` 的对象。
- 这一步是 **SentenceBag 中“如何从 SQL 结果行映射出 T”最核心的实现之一**，但它是由 `SetRunQuery` 间接调用 `BuildMapper` 组合出来的。

---

### 8. 总体流程小结：ExpressionBuilder → SentenceBag<T>

综合上面各节，可以把 `ExpressionBuilder` 构建 `SentenceBag<T>` 的流程总结为：

1. **构建序列（BuildSequence）**
   - 通过 `TryFindBuilder` 找到合适的 `ISequenceBuilder`。
   - 构建 `IBuildContext`，产生 `SelectQueryClause` 等 SQL 结构。

2. **生成 Statement + SentenceItem**
   - 使用 `sequence.GetResultStatement()` 获取 SQL 语法树 `Statement`。
   - 创建 `SentenceItem`，填充：
     - `Statement`。
     - `ParameterAccessors = ParametersContext.CurrentSqlParameters`。
   - 加入 `SentenceBag<T>.Sentences`。

3. **生成结果投影表达式**
   - 通过 `MakeExpression` 从 `ContextRefExpression` 出发生成结果投影 `expr`。

4. **FinalizeProjection**
   - 处理实体构造（`FinalizeConstructors`）。
   - 处理急切加载与前置查询（`CompleteEagerLoadingExpressions` + `preambles`）。
   - 绑定列信息（`ToColumns`），形成带列位置信息的表达式。

5. **错误检测 + SQL Finalize + 查询合法性校验**
   - 发现错误则写入 `SentenceBag.ErrorExpression`。
   - 使用 `SqlOptimizer.Finalize` 优化 SQL。
   - 使用 `SqlProviderHelper.IsValidQuery` 校验 SQL 合法性。

6. **设置执行行为（SetRunQuery）**
   - 在 `IBuildContext` 内为 `SentenceBag<T>.Runner` 配置：
     - 如何根据 `SentenceBag` + `RunnerContext` 构造 `SentenceCmds`。
     - 如何基于 `DbDataReader` + 投影表达式构造 `T`（通过 `BuildMapper` 等）。

7. **返回 SentenceBag<T>**
   - `doBuild<T>` 返回构建完毕的 `SentenceBag<T>`。
   - 若 `ErrorExpression` 非空，则 `QueryMate.CreateQuery` 会再以 `validateSubqueries = true` 重试一次，否则抛出“表达式编译错误”。

---

### 9. 与 EntityVisitCompiler 的衔接关系

- 在 `EntityVisitCompiler.DoCompile` 中：
  - `QueryMate.GetQuery<TResult>` 内部调用 `ExpressionBuilder.doBuild<TResult>`，生成 `SentenceBag<TResult>`。
  - `SentenceBag<TResult>` 携带：
    - 完整 SQL 模型（`Sentences` / `Statement`）。
    - 参数访问器（`ParameterAccessors`）。
    - 投影/映射表达式（`finalExp`，内部含列位置信息）。
    - 已配置好的 Runner（`SentenceBag<TResult>.Runner`）。
    - 前置查询（`Preambles`）。
- 随后，`EntityVisitCompiler` 只需要：
  - 调用 `InitPreambles` 执行前置查询。
  - 构建 `RunnerContext`。
  - 调用 `Runner.loadElement` / `loadElementAsync` 获得最终 `TResult`。

> 换句话说：**`ExpressionBuilder` 把“LINQ 表达式 + DB 环境”编译成一个“可直接执行的查询包 `SentenceBag<T>`”，`EntityVisitCompiler` 只是把这个“查询包”跑起来。**

