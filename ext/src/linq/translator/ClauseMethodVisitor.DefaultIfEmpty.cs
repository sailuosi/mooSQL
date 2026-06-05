using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitDefaultIfEmpty(DefaultIfEmptyCall method) => VisitDefaultIfEmptyCore(method);

    MethodCall VisitDefaultIfEmptyCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!DefaultIfEmptyBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder))
        {
            Context.BuildResult = BuildSequenceResult.NotSupported();
            return method;
        }

        Context.BuildResult = DefaultIfEmptyBuilder.Compile(Context.Builder, buildInfo);
        return method;
    }
}
