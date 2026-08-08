using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.DataProvider;
using mooSQL.linq.Linq;
using mooSQL.linq.SqlQuery;
using mooSQL.linq.Tools;

namespace mooSQL.linq.translator;

/// <summary>
/// Statement → ClauseTranslateVisitor → <see cref="SQLBuilderClause.ToCmd"/> → 执行；
/// 实体映射使用 <see cref="SQLBuilder.exeQuery{T}(SQLCmd)"/>。
/// </summary>
internal static partial class SentenceExecutor
{
    public static TResult Execute<TResult>(SentenceBag bag, QueryContext context, Expression expression, object?[]? parameters = null)
    {
        if (bag.Sentences == null || bag.Sentences.Count == 0)
            throw new InvalidOperationException("SentenceBag has no statements to execute.");

        var db = context.DB ?? bag.DBLive;

        var writeResult = ExecuteWriteOrAlternative(bag, db, expression, parameters);
        if (writeResult != null)
            return (TResult)writeResult;

        var resultType = typeof(TResult);
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = resultType.GetGenericArguments()[0];
            return (TResult)ExecuteEnumerable(elementType, bag, db, expression, context, parameters)!;
        }

        if (typeof(IQueryable).IsAssignableFrom(resultType) || typeof(IEnumerable).IsAssignableFrom(resultType))
        {
            var elementType = bag.EntityType ?? typeof(object);
            var list = ExecuteEnumerable(elementType, bag, db, expression, context, parameters);
            return (TResult)list!;
        }

        return ExecuteScalar<TResult>(bag, db, expression, parameters);
    }

    static object ExecuteEnumerable(Type elementType, SentenceBag bag, DBInstance db, Expression expression, QueryContext context, object?[]? parameters = null)
    {
        FinalizeBag(bag, db);
        var (kit, cmd) = BuildSelectCmd(bag, db, expression, parameters);
        var method = typeof(SentenceExecutor).GetMethod(nameof(QueryAndLoadNav), BindingFlags.NonPublic | BindingFlags.Static)!;
        return method.MakeGenericMethod(elementType).Invoke(null, new object[] { kit, cmd, bag })!;
    }

    static List<T> QueryAndLoadNav<T>(SQLBuilder kit, SQLCmd cmd, SentenceBag bag)
    {
        var res = kit.exeQuery<T>(cmd).ToList();
        NavColumnLoader.LoadNavChilds(bag, res);
        return res;
    }

    static TResult ExecuteScalar<TResult>(SentenceBag bag, DBInstance db, Expression expression, object?[]? parameters = null)
    {
        FinalizeBag(bag, db);
        var (kit, cmd) = BuildSelectCmd(bag, db, expression, parameters);
        var t = typeof(TResult);

        if (t == typeof(int) || t == typeof(long) || t == typeof(bool))
            return (TResult)Convert.ChangeType(kit.exeQueryCount(cmd), t)!;

        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            return (TResult)Convert.ChangeType(kit.DBLive.ExeQueryScalar<object>(cmd, kit.Executor), t)!;

        return kit.DBLive.ExeQueryUniqueRow<TResult>(cmd, kit.Executor);
    }

    static async Task<TResult> ExecuteScalarAsync<TResult>(SentenceBag bag, DBInstance db, Expression expression, CancellationToken cancellationToken)
    {
        FinalizeBag(bag, db);
        var (kit, cmd) = BuildSelectCmd(bag, db, expression);
        var t = typeof(TResult);

        if (t == typeof(int) || t == typeof(long) || t == typeof(bool))
        {
            var count = await kit.exeQueryCountAsync(cmd).ConfigureAwait(false);
            return (TResult)Convert.ChangeType(count, t)!;
        }

        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            return (TResult)Convert.ChangeType(
                await kit.DBLive.ExeQueryScalarAsync<object>(cmd, kit.Executor).ConfigureAwait(false), t)!;

        return await kit.DBLive.ExeQueryUniqueRowAsync<TResult>(cmd, kit.Executor).ConfigureAwait(false);
    }

    /// <summary>
    /// 翻译为已填充的 <see cref="SQLBuilder"/>（供 ToSQLBuilder / 桥接）；始终 Visit，顺带捕获 L2。
    /// </summary>
    internal static SQLBuilder BuildSqlBuilderPublic(SentenceBag bag, DBInstance db, Expression expression, object?[]? parameters = null)
        => VisitToBuilder(bag, db, expression, parameters, captureL2: true);

    /// <summary>
    /// L2 优先得到 <see cref="SQLCmd"/>；未命中则 <see cref="SQLBuilderClause.ToCmd"/>。
    /// SQLBuilder 只作拼装/执行载体，不承载命令缓存。
    /// </summary>
    static (SQLBuilder kit, SQLCmd cmd) BuildSelectCmd(SentenceBag bag, DBInstance db, Expression expression, object?[]? parameters = null)
    {
        var sentence = bag.Sentences[0];
        var parameterValues = new SqlParameterValues();
        QueryMate.SetParameters(bag, expression, db, parameters, sentence, parameterValues);

        if (ExtSqlCmdL2.TryBuild(sentence, parameterValues, out var cached) && cached != null)
            return (db.useSQL(), cached);

        var builderClause = VisitToClause(db, sentence.Statement, parameterValues);
        var cmd = builderClause.ToCmd();
        ExtSqlCmdL2.TryCapture(sentence, cmd, parameterValues);
        return (builderClause.Builder, cmd);
    }

    /// <summary>Visit 填充 Builder；可选捕获 L2 模板。</summary>
    static SQLBuilder VisitToBuilder(
        SentenceBag bag, DBInstance db, Expression expression, object?[]? parameters, bool captureL2)
    {
        var sentence = bag.Sentences[0];
        var parameterValues = new SqlParameterValues();
        QueryMate.SetParameters(bag, expression, db, parameters, sentence, parameterValues);

        var builderClause = VisitToClause(db, sentence.Statement, parameterValues);
        if (captureL2)
        {
            var cmd = builderClause.ToCmd();
            ExtSqlCmdL2.TryCapture(sentence, cmd, parameterValues);
        }

        return builderClause.Builder;
    }

    static SQLBuilderClause VisitToClause(DBInstance db, BaseSentence statement, SqlParameterValues parameterValues)
    {
        var translator = db.dialect.clauseTranslator.Prepare(db);
        translator.ParameterValues = parameterValues;
        var clause = translator.Visit(statement);

        if (clause is not SQLBuilderClause builderClause)
            throw new InvalidOperationException(
                $"Clause translation expected {nameof(SQLBuilderClause)} but got {clause?.GetType().Name ?? "null"}.");

        return builderClause;
    }

    public static string GetSqlText(SentenceBag bag, DBInstance db, Expression expression, object?[]? parameters = null)
    {
        EnsureInsertOrUpdateExpanded(bag, db);
        FinalizeBag(bag, db);

        var context = CreateContext(bag, db, expression, parameters);
        var cmds = PrepareCommands(context);
        return string.Join(Environment.NewLine, cmds.Select(c => c.sql));
    }

    public static object? ExecuteObject(SentenceBag bag, DBInstance db, Expression expression, object?[]? parameters = null)
    {
        var context = new QueryContext { DB = db };
        var resultType = expression.Type;

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            resultType = resultType.GetGenericArguments()[0];

        var method = typeof(SentenceExecutor).GetMethod(nameof(Execute), BindingFlags.Public | BindingFlags.Static)!;
        return method.MakeGenericMethod(resultType).Invoke(null, new object[] { bag, context, expression, parameters });
    }

    public static async Task<object?> ExecuteObjectAsync(
        SentenceBag bag, DBInstance db, Expression expression, CancellationToken cancellationToken = default, object?[]? parameters = null)
    {
        var resultType = expression.Type;
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            resultType = resultType.GetGenericArguments()[0];

        var writeResult = await ExecuteWriteOrAlternativeAsync(bag, db, expression, cancellationToken, parameters)
            .ConfigureAwait(false);
        if (writeResult != null)
            return writeResult;

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return await AwaitGenericTask(
                ExecuteListAsyncMethod.MakeGenericMethod(resultType.GetGenericArguments()[0]),
                bag, db, expression, cancellationToken, parameters).ConfigureAwait(false);

        if (typeof(IQueryable).IsAssignableFrom(resultType) || typeof(IEnumerable).IsAssignableFrom(resultType))
        {
            var elementType = bag.EntityType ?? typeof(object);
            return await AwaitGenericTask(ExecuteListAsyncMethod.MakeGenericMethod(elementType),
                bag, db, expression, cancellationToken, parameters).ConfigureAwait(false);
        }

        return await AwaitGenericTask(ExecuteScalarAsyncMethod.MakeGenericMethod(resultType),
            bag, db, expression, cancellationToken, parameters).ConfigureAwait(false);
    }

    static readonly MethodInfo ExecuteListAsyncMethod =
        typeof(SentenceExecutor).GetMethod(nameof(ExecuteListAsync), BindingFlags.Public | BindingFlags.Static)!;

    static readonly MethodInfo ExecuteScalarAsyncMethod =
        typeof(SentenceExecutor).GetMethod(nameof(ExecuteScalarAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    static async Task<object?> AwaitGenericTask(
        MethodInfo method, SentenceBag bag, DBInstance db, Expression expression,
        CancellationToken cancellationToken, object?[]? parameters = null)
    {
        var task = (Task)method.Invoke(null, new object[] { bag, db, expression, cancellationToken, parameters })!;
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task);
    }

    public static List<T> ExecuteList<T>(SentenceBag bag, DBInstance db, Expression expression, object?[]? parameters = null)
    {
        FinalizeBag(bag, db);
        var (kit, cmd) = BuildSelectCmd(bag, db, expression, parameters);
        var res = kit.exeQuery<T>(cmd).ToList();
        NavColumnLoader.LoadNavChilds(bag, res);
        return res;
    }

    public static async Task<List<T>> ExecuteListAsync<T>(
        SentenceBag bag, DBInstance db, Expression expression, CancellationToken cancellationToken = default, object?[]? parameters = null)
    {
        FinalizeBag(bag, db);
        var (kit, cmd) = BuildSelectCmd(bag, db, expression, parameters);
        var res = (await kit.exeQueryAsync<T>(cmd).ConfigureAwait(false)).ToList();
        NavColumnLoader.LoadNavChilds(bag, res);
        return res;
    }

    /// <summary>测试/诊断：当前 bag 首句是否已捕获 L2 模板。</summary>
    internal static bool HasL2Template(SentenceBag bag)
        => bag.Sentences is { Count: > 0 } && bag.Sentences[0].L2Template != null;

    internal static void FinalizeBag(SentenceBag bag, DBInstance db)
    {
        if (bag.IsFinalized)
            return;

        EntitySelectProjector.Apply(bag);

        var optimizer = SqlOptimizerFactory.Get(db);
        foreach (var sentence in bag.Sentences)
        {
            sentence.Statement = optimizer.Finalize(db, sentence.Statement);
            if (sentence.Statement.SelectQuery != null
                && !SqlProviderHelper.IsValidQuery(sentence.Statement.SelectQuery, null, null, false,
                    db.dialect.Option.ProviderFlags, out var errorMessage))
            {
                throw new LinqException(errorMessage);
            }
        }

        bag.IsFinalized = true;
    }
}
