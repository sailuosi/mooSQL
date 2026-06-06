using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Linq;

namespace mooSQL.linq.translator;

public static partial class LinqStatementCompiler
{
    /// <summary>
    /// 将 LINQ 表达式编译并翻译为 <see cref="SQLBuilder"/>（首条语句；多语句见 <see cref="ToSQLBuilders"/>）。
    /// </summary>
    public static SQLBuilder ToSQLBuilder(
        DBInstance db,
        Expression expression,
        object?[]? parameters = null)
        => ToSQLBuilders(db, expression, parameters).FirstOrDefault()
           ?? throw new InvalidOperationException("Compile produced no SQLBuilder statements.");

    /// <summary>编译全部 <see cref="SentenceBag.Sentences"/> 为 SQLBuilder 列表。</summary>
    public static IReadOnlyList<SQLBuilder> ToSQLBuilders(
        DBInstance db,
        Expression expression,
        object?[]? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(expression);

        var elementType = ResolveElementType(expression) ?? typeof(object);
        var method = typeof(LinqStatementCompiler)
            .GetMethod(nameof(ToSQLBuildersTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(elementType);

        return (IReadOnlyList<SQLBuilder>)method.Invoke(null, [db, expression, parameters])!;
    }

    /// <summary>
    /// 从编译结果获取 SQL 预览文本。
    /// </summary>
    public static string GetSqlText(DBInstance db, Expression expression, object?[]? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(expression);

        var elementType = ResolveElementType(expression) ?? typeof(object);
        var method = typeof(LinqStatementCompiler)
            .GetMethod(nameof(GetSqlTextTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(elementType);

        return (string)method.Invoke(null, [db, expression, parameters])!;
    }

    static IReadOnlyList<SQLBuilder> ToSQLBuildersTyped<T>(DBInstance db, Expression expression, object?[]? parameters)
    {
        var expr = expression;
        var bag = QueryMate.GetQuery<T>(db, ref expr, out _);
        bag.DBLive = db;
        bag.srcExp = expr;
        SentenceExecutor.FinalizeBag(bag, db);

        if (bag.Sentences == null || bag.Sentences.Count == 0)
            return Array.Empty<SQLBuilder>();

        var kits = new List<SQLBuilder>(bag.Sentences.Count);
        foreach (var sentence in bag.Sentences)
        {
            var single = new SentenceBag { DBLive = db, srcExp = expr, Sentences = new List<SentenceItem> { sentence } };
            var kit = SentenceExecutor.BuildSqlBuilderPublic(single, db, expr, parameters);
            if (sentence.Statement is SelectSentence selectSentence)
                LinqClauseBridge.AttachSelectQuery(kit, selectSentence.SelectQuery);
            kits.Add(kit);
        }

        return kits;
    }

    static SQLBuilder ToSQLBuilderTyped<T>(DBInstance db, Expression expression, object?[]? parameters)
        => ToSQLBuildersTyped<T>(db, expression, parameters).FirstOrDefault()
           ?? throw new InvalidOperationException("Compile produced no SQLBuilder statements.");

    static string GetSqlTextTyped<T>(DBInstance db, Expression expression, object?[]? parameters)
    {
        var expr = expression;
        var bag = QueryMate.GetQuery<T>(db, ref expr, out _);
        bag.DBLive = db;
        bag.srcExp = expr;
        return SentenceExecutor.GetSqlText(bag, db, expr, parameters);
    }
}
