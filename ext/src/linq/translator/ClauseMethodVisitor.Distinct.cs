using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Reflection;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitDistinct(DistinctCall method) => VisitDistinctCore(method);
    public override MethodCall VisitSelectDistinct(SelectDistinctCall method) => VisitDistinctCore(method);

    MethodCall VisitDistinctCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!DistinctBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder))
            return method;

        var buildResult = Context.Builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        if (buildResult.BuildContext == null)
            return method;

        var sequence = buildResult.BuildContext;
        var sql = sequence.SelectQuery;
        if (sql.Select.TakeValue != null || sql.Select.SkipValue != null)
            sequence = new SubQueryContext(sequence);

        var subQueryContext = new SubQueryContext(sequence);
        subQueryContext.SelectQuery.Select.IsDistinct = true;

        var outerSubqueryContext = new SubQueryContext(subQueryContext);

        if (methodCall.IsSameGenericMethod(Methods.LinqToDB.SelectDistinct))
        {
            subQueryContext.SelectQuery.Select.OptimizeDistinct = true;
        }
        else
        {
            var sqlExpr = Context.Builder.BuildSqlExpression(
                outerSubqueryContext,
                new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], subQueryContext),
                buildInfo.GetFlags());
            SequenceHelper.EnsureNoErrors(sqlExpr);
            _ = Context.Builder.UpdateNesting(outerSubqueryContext, sqlExpr);
        }

        return ToStatementCallOr(method, new DistinctBuilder.DistinctContext(outerSubqueryContext));
    }
}
