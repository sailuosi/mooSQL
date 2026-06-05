using mooSQL.data.call;
using mooSQL.linq;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitContains(ContainsCall method) => VisitContainsCore(method);

    MethodCall VisitContainsCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!ContainsBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder)
            && !ContainsBuilder.CanBuildAsyncMethod(methodCall, buildInfo, Context.Builder))
            return method;

        var innerQuery = new mooSQL.data.model.SelectQueryClause();
        var buildResult = Context.Builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], innerQuery));
        if (buildResult.BuildContext == null)
            return method;

        var sequence = new SubQueryContext(buildResult.BuildContext);
        var containsContext = new ContainsBuilder.ContainsContext(buildInfo.Parent, methodCall, buildInfo.SelectQuery, sequence);
        if (containsContext.TryCreatePlaceholder() == null)
            return method;

        return ToStatementCallOr(method, containsContext);
    }
}
