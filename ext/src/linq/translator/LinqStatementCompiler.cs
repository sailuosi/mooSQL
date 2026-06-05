using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Linq;

using mooSQL.linq;

namespace mooSQL.linq.translator;

/// <summary>
/// translator 模块公共入口：将 LINQ 表达式编译为 Statement / SqlPlan，供非 DbBus 场景复用。
/// </summary>
public static class LinqStatementCompiler
{
    /// <summary>编译表达式为 Statement 结构与 SqlPlan（不执行查询）。</summary>
    public static StatementCompileResult Compile(
        DBInstance db,
        Expression expression,
        StatementCompileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(expression);

        options ??= new StatementCompileOptions();
        var stages = new List<string> { "Expression.Optimize", "ExpressionBuilder.Build" };

        var elementType = ResolveElementType(expression) ?? typeof(object);
        var compileMethod = typeof(LinqStatementCompiler)
            .GetMethod(nameof(CompileTyped), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(elementType);

        return (StatementCompileResult)compileMethod.Invoke(null, [db, expression, options, stages])!;
    }

    static StatementCompileResult CompileTyped<T>(
        DBInstance db,
        Expression expression,
        StatementCompileOptions options,
        List<string> stages)
    {
        var expr = expression;
        var bag  = QueryMate.GetQuery<T>(db, ref expr, out _);
        bag.DBLive = db;
        bag.srcExp = expr;

        SelectQueryClause? primary = null;
        if (bag.Sentences is { Count: > 0 })
            primary = bag.Sentences[0].Statement.SelectQuery;

        var plan = SqlPlanBuilder.Build(bag, db, expr, options, stages);

        return new StatementCompileResult
        {
            ErrorExpression    = bag.ErrorExpression,
            Expression         = expr,
            Plan               = plan,
            PrimarySelectQuery = primary
        };
    }

    static Type? ResolveElementType(Expression expression)
    {
        var type = expression.Type;
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(IQueryable<>) || def == typeof(IOrderedQueryable<>))
                return type.GetGenericArguments()[0];
        }

        return type.TryGetSequenceType();
    }
}
