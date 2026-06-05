using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitLoadWith(LoadWithCall method) => VisitLoadWithCore(method);
    public override MethodCall VisitThenLoad(ThenLoadCall method) => VisitLoadWithCore(method);
    public override MethodCall VisitLoadWithAsTable(LoadWithAsTableCall method) => VisitLoadWithCore(method);
    public override MethodCall VisitLoadWithInternal(LoadWithInternalCall method) => VisitLoadWithCore(method);

    MethodCall VisitLoadWithCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!LoadWithBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder))
        {
            Context.BuildResult = BuildSequenceResult.NotSupported();
            return method;
        }

        Context.BuildResult = LoadWithBuilder.Compile(Context.Builder, buildInfo);
        return method;
    }
}
