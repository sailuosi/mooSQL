using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using mooSQL.linq.Extensions;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Reflection;
using mooSQL.utils;

namespace mooSQL.linq.translator;

internal sealed partial class ClauseExpressionVisitor
{
    static bool CanBuildEnumerable(Expression expr, BuildInfo info, ClauseSqlTranslator builder)
    {
        if (expr.NodeType == ExpressionType.NewArrayInit)
            return true;

        if (typeof(IQueryable).IsAssignableFrom(expr.Type))
            return false;

        if (!typeof(IEnumerable<>).IsSameOrParentOf(expr.Type))
            return false;

        if (typeof(IEnumerable<>).GetGenericType(expr.Type) is null)
            return false;

        return expr.NodeType switch
        {
            ExpressionType.MemberAccess => CanBuildMemberChain(((MemberExpression)expr).Expression),
            ExpressionType.Constant => ((ConstantExpression)expr).Value is IEnumerable,
            _ => false,
        };

        static bool CanBuildMemberChain(Expression? memberExpr)
        {
            while (memberExpr is { NodeType: ExpressionType.MemberAccess })
                memberExpr = ((MemberExpression)memberExpr).Expression;

            return memberExpr is null or { NodeType: ExpressionType.Constant };
        }
    }

    bool TryVisitEnumerable(Expression node, BuildInfo buildInfo)
    {
        if (!CanBuildEnumerable(node, buildInfo, Context.Builder))
            return false;

        SetStatementResult(BuildEnumerableCore(Context.Builder, buildInfo).BuildContext);
        return Context.StatementResult != null;
    }

    static BuildSequenceResult BuildEnumerableCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
    {
        var collectionType = typeof(IEnumerable<>).GetGenericType(buildInfo.Expression.Type) ??
                             throw new InvalidOperationException();

        var enumerableContext = new EnumerableContext(builder, buildInfo, buildInfo.SelectQuery, collectionType.GetGenericArguments()[0]);

        return BuildSequenceResult.FromContext(enumerableContext);
    }
}
