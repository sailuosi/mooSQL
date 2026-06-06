using System;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Extensions;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.SqlQuery;
using mooSQL.utils;

namespace mooSQL.linq.translator;

internal sealed partial class ClauseExpressionVisitor
{
    static bool CanBuildTableAttributed(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
        => call.Method.GetTableFunctionAttribute(builder.DBLive) != null;

    bool TryVisitTableAttributed(MethodCallExpression node, BuildInfo buildInfo)
    {
        if (!CanBuildTableAttributed(node, buildInfo, Context.Builder))
            return false;

        SetStatementResult(BuildTableFunctionCore(Context.Builder, buildInfo).BuildContext);
        return Context.StatementResult != null;
    }

    internal static BuildSequenceResult BuildTableFunctionCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
        => BuildSequenceResult.FromContext(new TableContext(builder, builder.DBLive, buildInfo));

    internal static BuildSequenceResult BuildUseQueryableCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
        => BuildTableWithAppliedFilters(builder, buildInfo, builder.DBLive, buildInfo.Expression);

    internal static BuildSequenceResult BuildTableFromExpressionCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
    {
        var mc = (MethodCallExpression)buildInfo.Expression;
        var bodyMethod = mc.Arguments[1].UnwrapLambda().Body;
        return BuildSequenceResult.FromContext(new TableContext(builder, builder.DBLive, new BuildInfo(buildInfo, bodyMethod)));
    }

    internal static BuildSequenceResult BuildAsCteCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
        => BuildCteContext(builder, buildInfo);

    internal static BuildSequenceResult BuildGetCteCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
        => BuildRecursiveCteContextTable(builder, buildInfo);

    internal static BuildSequenceResult BuildFromSqlCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
        => TableRawSqlHelper.BuildRawSqlTable(builder, buildInfo, false);

    internal static BuildSequenceResult BuildFromSqlScalarCore(ClauseSqlTranslator builder, BuildInfo buildInfo)
        => TableRawSqlHelper.BuildRawSqlTable(builder, buildInfo, true);

    static Expression ApplyQueryFilters(ClauseSqlTranslator builder, DBInstance DB, Type entityType, Expression tableExpression)
        => tableExpression;

    static BuildSequenceResult BuildTableWithAppliedFilters(ClauseSqlTranslator builder, BuildInfo buildInfo, DBInstance mappingSchema, Expression tableExpression)
    {
        var entityType = tableExpression.Type.GetGenericArguments()[0];
        var applied = ApplyQueryFilters(builder, builder.DBLive, entityType, tableExpression);

        if (!ReferenceEquals(applied, tableExpression))
            return builder.TryBuildSequence(new BuildInfo(buildInfo, applied));

        var tableContext = new TableContext(builder, buildInfo, entityType);
        builder.TablesInScope?.Add(tableContext);
        return BuildSequenceResult.FromContext(tableContext);
    }

    static BuildSequenceResult BuildCteContext(ClauseSqlTranslator builder, BuildInfo buildInfo)
    {
        var methodCall = (MethodCallExpression)buildInfo.Expression;

        var cteContext  = builder.FindRegisteredCteContext(methodCall);
        var elementType = methodCall.Method.GetGenericArguments()[0];

        if (cteContext == null)
        {
            string? tableName = null;

            var cteBody = methodCall.Arguments[0].Unwrap();
            if (methodCall.Arguments.Count > 1)
                tableName = methodCall.Arguments[1].EvaluateExpression<string>();

            var cteClause = new CTEClause(null, elementType, true, tableName);
            cteContext               = new CteContext(builder, null, cteClause, null!);
            cteContext.CteExpression = cteBody;

            builder.RegisterCteContext(cteContext, methodCall);
        }

        var cteTableContext = new CteTableContext(builder, buildInfo.Parent, elementType, buildInfo.SelectQuery, cteContext, buildInfo.IsTest);

        return BuildSequenceResult.FromContext(cteTableContext);
    }

    static BuildSequenceResult BuildRecursiveCteContextTable(ClauseSqlTranslator builder, BuildInfo buildInfo)
    {
        var methodCall = ((MethodCallExpression)buildInfo.Expression);

        var cteContext  = builder.FindRegisteredCteContext(methodCall);
        var elementType = methodCall.Method.GetGenericArguments()[0];

        if (cteContext == null)
        {
            var parameters      = methodCall.Method.GetParameters();
            var isSecondVariant = parameters[1].ParameterType == typeof(string);

            var lambda    = methodCall.Arguments[isSecondVariant ? 2 : 1].UnwrapLambda();
            var tableName = builder.EvaluateExpression<string>(methodCall.Arguments[isSecondVariant ? 1 : 2]);

            var cteClause  = new CTEClause(null, elementType, true, tableName);
            cteContext = new CteContext(builder, null, cteClause, null!);

            var cteBody = lambda.Body.Transform(e =>
            {
                if (e == lambda.Parameters[0])
                {
                    var cteTableContext    = new CteTableContext(builder, null, elementType, new SelectQueryClause(), cteContext, buildInfo.IsTest);
                    var cteTableContextRef = new ContextRefExpression(e.Type, cteTableContext);
                    return cteTableContextRef;
                }

                return e;
            });

            cteContext.CteExpression = cteBody;
            builder.RegisterCteContext(cteContext, methodCall);
        }

        var cteTableContext = new CteTableContext(builder, buildInfo.Parent, elementType, buildInfo.SelectQuery, cteContext, buildInfo.IsTest);

        return BuildSequenceResult.FromContext(cteTableContext);
    }
}
