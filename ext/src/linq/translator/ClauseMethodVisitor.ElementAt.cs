using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitElementAt(ElementAtCall method) => VisitElementAtCore(method);
    public override MethodCall VisitElementAtOrDefault(ElementAtOrDefaultCall method) => VisitElementAtCore(method);

    MethodCall VisitElementAtCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!ElementAtBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, ElementAtBuilder.Compile(Context.Builder, buildInfo).BuildContext);
    }
}
