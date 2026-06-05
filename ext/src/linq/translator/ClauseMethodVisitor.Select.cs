using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitSelect(SelectCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!SelectBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder))
        {
            Context.BuildResult = BuildSequenceResult.NotSupported();
            return method;
        }

        var selector = (LambdaExpression)methodCall.Arguments[1].Unwrap();
        var buildResult = Context.Builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        if (buildResult.BuildContext == null)
        {
            Context.BuildResult = buildResult;
            return method;
        }

        var sequence = buildResult.BuildContext;

        _ = Context.Builder.MakeExpression(sequence, new ContextRefExpression(sequence.ElementType, sequence),
            ProjectFlags.ExtractProjection);

        sequence.SetAlias(selector.Parameters[0].Name);

        var body = selector.Parameters.Count == 1
            ? SequenceHelper.PrepareBody(selector, sequence)
            : SequenceHelper.PrepareBody(selector, sequence, new SelectCounterContext(sequence));

        var context = new SelectContext(buildInfo.Parent, body, sequence, buildInfo.IsSubQuery);
#if DEBUG
        context.Debug_MethodCall = methodCall;
#endif
        Context.BuildResult = BuildSequenceResult.FromContext(context);
        return method;
    }
}
