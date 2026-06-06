using mooSQL.data.call;
using mooSQL.linq.Expressions;
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
        if (!CanBuildContains(methodCall, Context.Builder)
            && !CanBuildContainsAsync(methodCall, Context.Builder))
            return method;

        var innerQuery = new mooSQL.data.model.SelectQueryClause();
        var source = ResolveSourceContext(methodCall, buildInfo,
            new BuildInfo(buildInfo, methodCall.Arguments[0], innerQuery));
        if (source == null)
            return method;

        var sequence = new SubQueryContext(source);
        var containsContext = new ContainsContext(buildInfo.Parent, methodCall, buildInfo.SelectQuery, sequence);
        if (containsContext.TryCreatePlaceholder() == null)
            return method;

        return ToStatementCallOr(method, containsContext);
    }

    static bool CanBuildContains(MethodCallExpression call, ClauseSqlTranslator builder)
        => call.IsQueryable()
           && call.Arguments.Count == 2
           && !builder.CanBeCompiled(call.Arguments[0], false);

    static bool CanBuildContainsAsync(MethodCallExpression call, ClauseSqlTranslator builder)
        => call.IsAsyncExtension()
           && call.Arguments.Count == 3
           && !builder.CanBeCompiled(call.Arguments[0], false);
}
