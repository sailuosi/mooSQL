using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitFirst(FirstCall method) => VisitFirstSingle(method);
    public override MethodCall VisitFirstOrDefault(FirstOrDefaultCall method) => VisitFirstSingle(method);
    public override MethodCall VisitSingle(SingleCall method) => VisitFirstSingle(method);
    public override MethodCall VisitSingleOrDefault(SingleOrDefaultCall method) => VisitFirstSingle(method);

    MethodCall VisitFirstSingle(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!FirstSingleBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder)
            && !FirstSingleBuilder.CanBuildAsyncMethod(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, FirstSingleBuilder.Compile(Context.Builder, buildInfo).BuildContext);
    }
}
