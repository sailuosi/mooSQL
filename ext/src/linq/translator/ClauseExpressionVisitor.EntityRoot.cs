using System;
using System.Linq.Expressions;
using mooSQL.linq;
using mooSQL.linq.Linq.Builder;
using mooSQL.utils;

namespace mooSQL.linq.translator;

internal sealed partial class ClauseExpressionVisitor
{
    static bool CanBuildEntityRoot(Expression expr, BuildInfo info, ClauseSqlTranslator builder)
    {
        if (expr.NodeType != ExpressionType.Constant)
            return false;

        var type = expr.Type;
        if (!type.IsGenericType)
            return false;

        var genericDef = type.GetGenericTypeDefinition();
        return genericDef == typeof(EntityQueryable<>);
    }

    bool TryVisitEntityRoot(ConstantExpression node, BuildInfo buildInfo)
    {
        if (!CanBuildEntityRoot(node, buildInfo, Context.Builder))
            return false;

        SetStatementResult(BuildEntityRootCore(Context.Builder, buildInfo).BuildContext);
        return Context.StatementResult != null;
    }

    static BuildSequenceResult BuildEntityRootCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
    {
        var entityType = buildInfo.Expression.Type.GetGenericArguments()[0];
        var tableContext = new TableContext(builder, buildInfo, entityType);
        builder.TablesInScope?.Add(tableContext);
        return BuildSequenceResult.FromContext(tableContext);
    }
}
