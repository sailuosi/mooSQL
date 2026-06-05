using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitGroupJoin(GroupJoinCall method) => VisitGroupJoinCore(method);

    MethodCall VisitGroupJoinCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!GroupJoinBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, GroupJoinBuilder.Compile(Context.Builder, buildInfo).BuildContext);
    }
}
