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
/// Statement → ClauseTranslateVisitor → SQLBuilder → 执行；实体映射使用 query&lt;T&gt;()。
/// </summary>
internal static partial class SentenceExecutor
{
    public static TResult Execute<TResult>(SentenceBag bag, QueryContext context, Expression expression)
    {
        if (bag.Sentences == null || bag.Sentences.Count == 0)
            throw new InvalidOperationException("SentenceBag has no statements to execute.");

        var db = context.DB ?? bag.DBLive;

        var writeResult = ExecuteWriteOrAlternative(bag, db, expression);
        if (writeResult != null)
            return (TResult)writeResult;

        var resultType = typeof(TResult);
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = resultType.GetGenericArguments()[0];
            var method = typeof(SentenceExecutor).GetMethod(nameof(ExecuteEnumerable), BindingFlags.NonPublic | BindingFlags.Static)!;
            return (TResult)method.MakeGenericMethod(elementType).Invoke(null, new object[] { bag, db, expression, context })!;
        }

        if (typeof(IQueryable).IsAssignableFrom(resultType) || typeof(IEnumerable).IsAssignableFrom(resultType))
        {
            var elementType = bag.EntityType ?? typeof(object);
            var list = ExecuteEnumerable(elementType, bag, db, expression, context);
            return (TResult)list!;
        }

        return ExecuteScalar<TResult>(bag, db, expression);
    }

    static object ExecuteEnumerable(Type elementType, SentenceBag bag, DBInstance db, Expression expression, QueryContext context)
    {
        FinalizeBag(bag, db);
        var kit = BuildSqlBuilder(bag, db, expression);
        var method = typeof(SentenceExecutor).GetMethod(nameof(QueryAndLoadNav), BindingFlags.NonPublic | BindingFlags.Static)!;
        return method.MakeGenericMethod(elementType).Invoke(null, new object[] { kit, bag })!;
    }

    static List<T> QueryAndLoadNav<T>(SQLBuilder kit, SentenceBag bag)
    {
        var res = kit.query<T>().ToList();
        NavColumnLoader.LoadNavChilds(bag, res);
        return res;
    }

    static TResult ExecuteScalar<TResult>(SentenceBag bag, DBInstance db, Expression expression)
    {
        FinalizeBag(bag, db);
        var kit = BuildSqlBuilder(bag, db, expression);
        var t = typeof(TResult);

        if (t == typeof(int) || t == typeof(long) || t == typeof(bool))
            return (TResult)Convert.ChangeType(kit.count(), t)!;

        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            return (TResult)Convert.ChangeType(kit.queryScalar<object>(), t)!;

        return kit.queryUnique<TResult>();
    }

    static async Task<TResult> ExecuteScalarAsync<TResult>(SentenceBag bag, DBInstance db, Expression expression, CancellationToken cancellationToken)
    {
        FinalizeBag(bag, db);
        var kit = BuildSqlBuilder(bag, db, expression);
        var t = typeof(TResult);

        if (t == typeof(int) || t == typeof(long) || t == typeof(bool))
        {
            var count = await kit.exeQueryCountAsync(kit.toSelect()).ConfigureAwait(false);
            return (TResult)Convert.ChangeType(count, t)!;
        }

        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            return (TResult)Convert.ChangeType(await kit.queryScalarAsync<object>().ConfigureAwait(false), t)!;

        return await kit.queryUniqueAsync<TResult>().ConfigureAwait(false);
    }

    static SQLBuilder BuildSqlBuilder(SentenceBag bag, DBInstance db, Expression expression)
    {
        var sentence = bag.Sentences[0];
        var parameterValues = new SqlParameterValues();
        QueryMate.SetParameters(bag, expression, db, null, sentence, parameterValues);

        var translator = db.dialect.clauseTranslator.Prepare(db);
        var clause = translator.Visit(sentence.Statement);

        if (clause is SQLBuilderClause builderClause)
            return builderClause.Builder;

        throw new InvalidOperationException(
            $"Clause translation expected {nameof(SQLBuilderClause)} but got {clause?.GetType().Name ?? "null"}.");
    }

    public static string GetSqlText(SentenceBag bag, DBInstance db, Expression expression)
    {
        EnsureInsertOrUpdateExpanded(bag);
        FinalizeBag(bag, db);

        var context = CreateContext(bag, db, expression);
        var cmds = PrepareCommands(context);
        return string.Join(Environment.NewLine, cmds.Select(c => c.sql));
    }

    public static object? ExecuteObject(SentenceBag bag, DBInstance db, Expression expression)
    {
        var context = new QueryContext { DB = db };
        var resultType = expression.Type;

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            resultType = resultType.GetGenericArguments()[0];

        var method = typeof(SentenceExecutor).GetMethod(nameof(Execute), BindingFlags.Public | BindingFlags.Static)!;
        return method.MakeGenericMethod(resultType).Invoke(null, new object[] { bag, context, expression });
    }

    public static async Task<object?> ExecuteObjectAsync(
        SentenceBag bag, DBInstance db, Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = expression.Type;
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            resultType = resultType.GetGenericArguments()[0];

        var writeResult = await ExecuteWriteOrAlternativeAsync(bag, db, expression, cancellationToken)
            .ConfigureAwait(false);
        if (writeResult != null)
            return writeResult;

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return await AwaitGenericTask(
                ExecuteListAsyncMethod.MakeGenericMethod(resultType.GetGenericArguments()[0]),
                bag, db, expression, cancellationToken).ConfigureAwait(false);

        if (typeof(IQueryable).IsAssignableFrom(resultType) || typeof(IEnumerable).IsAssignableFrom(resultType))
        {
            var elementType = bag.EntityType ?? typeof(object);
            return await AwaitGenericTask(ExecuteListAsyncMethod.MakeGenericMethod(elementType),
                bag, db, expression, cancellationToken).ConfigureAwait(false);
        }

        return await AwaitGenericTask(ExecuteScalarAsyncMethod.MakeGenericMethod(resultType),
            bag, db, expression, cancellationToken).ConfigureAwait(false);
    }

    static readonly MethodInfo ExecuteListAsyncMethod =
        typeof(SentenceExecutor).GetMethod(nameof(ExecuteListAsync), BindingFlags.Public | BindingFlags.Static)!;

    static readonly MethodInfo ExecuteScalarAsyncMethod =
        typeof(SentenceExecutor).GetMethod(nameof(ExecuteScalarAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    static async Task<object?> AwaitGenericTask(
        MethodInfo method, SentenceBag bag, DBInstance db, Expression expression, CancellationToken cancellationToken)
    {
        var task = (Task)method.Invoke(null, new object[] { bag, db, expression, cancellationToken })!;
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task);
    }

    public static List<T> ExecuteList<T>(SentenceBag bag, DBInstance db, Expression expression)
    {
        FinalizeBag(bag, db);
        var kit = BuildSqlBuilder(bag, db, expression);
        var res = kit.query<T>().ToList();
        NavColumnLoader.LoadNavChilds(bag, res);
        return res;
    }

    public static async Task<List<T>> ExecuteListAsync<T>(
        SentenceBag bag, DBInstance db, Expression expression, CancellationToken cancellationToken = default)
    {
        FinalizeBag(bag, db);
        var kit = BuildSqlBuilder(bag, db, expression);
        var res = (await kit.queryAsync<T>().ConfigureAwait(false)).ToList();
        NavColumnLoader.LoadNavChilds(bag, res);
        return res;
    }

    static void FinalizeBag(SentenceBag bag, DBInstance db)
    {
        if (bag.IsFinalized)
            return;

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
