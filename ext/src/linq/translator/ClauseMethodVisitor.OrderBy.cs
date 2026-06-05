using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.SqlQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitOrderBy(OrderByCall method) => VisitOrderByCore(method);
    public override MethodCall VisitOrderByDescending(OrderByDescendingCall method) => VisitOrderByCore(method);
    public override MethodCall VisitThenBy(ThenByCall method) => VisitOrderByCore(method);
    public override MethodCall VisitThenByDescending(ThenByDescendingCall method) => VisitOrderByCore(method);
    public override MethodCall VisitThenOrBy(ThenOrByCall method) => VisitOrderByCore(method);
    public override MethodCall VisitThenOrByDescending(ThenOrByDescendingCall method) => VisitOrderByCore(method);

    MethodCall VisitOrderByCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!OrderByBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder))
            return method;

        var sequence = ResolveSourceContext(methodCall, buildInfo);
        if (sequence == null)
            return method;

        var wrapped = false;

        if (sequence.SelectQuery.Select.HasModifier)
        {
            sequence = new SubQueryContext(sequence);
            wrapped = true;
        }

        var orderByProjectFlags = ProjectFlags.SQL | ProjectFlags.Keys;
        var isContinuousOrder = !sequence.SelectQuery.OrderBy.IsEmpty && methodCall.Method.Name.StartsWith("Then");
        var lambda = (LambdaExpression)methodCall.Arguments[1].Unwrap();
        var byIndex = false;
        List<SqlPlaceholderExpression> placeholders;

        while (true)
        {
            Expression sqlExpr;
            var body = SequenceHelper.PrepareBody(lambda, sequence).Unwrap();

            if (body is MethodCallExpression mc && mc.Method.DeclaringType == typeof(Sql) && mc.Method.Name == nameof(Sql.Ordinal))
            {
                sqlExpr = Context.Builder.ConvertToSqlExpr(sequence, mc.Arguments[0], orderByProjectFlags);
                byIndex = true;
            }
            else
            {
                sqlExpr = Context.Builder.ConvertToSqlExpr(sequence, body, orderByProjectFlags);
                byIndex = false;
            }

            if (!SequenceHelper.IsSqlReady(sqlExpr))
                return method;

            placeholders = ClauseSqlTranslator.CollectDistinctPlaceholders(sqlExpr);

            if (wrapped || isContinuousOrder)
                break;

            var isComplex = false;
            foreach (var placeholder in placeholders)
            {
                if (QueryHelper.IsConstant(placeholder.Sql))
                    continue;

                isComplex = placeholder.Sql.Find(e => e.NodeType == ClauseType.SqlQuery || e.NodeType == ClauseType.SqlFunction) != null;
                if (isComplex)
                    break;
            }

            if (!isComplex)
                break;

            sequence = new SubQueryContext(sequence);
            wrapped = true;
        }

        if (!isContinuousOrder && !Context.Builder.DBLive.dialect.Option.DoNotClearOrderBys)
            sequence.SelectQuery.OrderBy.Items.Clear();

        foreach (var placeholder in placeholders)
        {
            var isPositioned = byIndex;
            sequence.SelectQuery.OrderBy.Expr(placeholder.Sql, methodCall.Method.Name.EndsWith("Descending"), isPositioned);
        }

        return ToStatementCallOr(method, sequence);
    }
}
