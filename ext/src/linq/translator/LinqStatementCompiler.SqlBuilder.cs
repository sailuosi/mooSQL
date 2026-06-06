using System;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.linq.Linq;

namespace mooSQL.linq.translator;

public static partial class LinqStatementCompiler
{
    /// <summary>
    /// 将 LINQ 表达式编译并翻译为 <see cref="SQLBuilder"/>（Clause IR → SQLBuilder 桥接）。
    /// </summary>
    public static SQLBuilder ToSQLBuilder(
        DBInstance db,
        Expression expression,
        object?[]? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(expression);

        var elementType = ResolveElementType(expression) ?? typeof(object);
        var compileMethod = typeof(LinqStatementCompiler)
            .GetMethod(nameof(ToSQLBuilderTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(elementType);

        return (SQLBuilder)compileMethod.Invoke(null, [db, expression, parameters])!;
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

    static SQLBuilder ToSQLBuilderTyped<T>(DBInstance db, Expression expression, object?[]? parameters)
    {
        var expr = expression;
        var bag = QueryMate.GetQuery<T>(db, ref expr, out _);
        bag.DBLive = db;
        bag.srcExp = expr;
        SentenceExecutor.FinalizeBag(bag, db);
        return SentenceExecutor.BuildSqlBuilderPublic(bag, db, expr, parameters);
    }

    static string GetSqlTextTyped<T>(DBInstance db, Expression expression, object?[]? parameters)
    {
        var expr = expression;
        var bag = QueryMate.GetQuery<T>(db, ref expr, out _);
        bag.DBLive = db;
        bag.srcExp = expr;
        return SentenceExecutor.GetSqlText(bag, db, expr, parameters);
    }
}
