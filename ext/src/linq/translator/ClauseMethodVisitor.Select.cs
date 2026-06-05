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

        var sequence = ResolveSourceContext(methodCall, buildInfo);
        if (sequence == null)
            return method;

        var selector = (LambdaExpression)methodCall.Arguments[1].Unwrap();

        _ = Context.Translator.MakeExpression(sequence, new ContextRefExpression(sequence.ElementType, sequence),
            ProjectFlags.ExtractProjection);

        sequence.SetAlias(selector.Parameters[0].Name);

        var body = selector.Parameters.Count == 1
            ? SequenceHelper.PrepareBody(selector, sequence)
            : SequenceHelper.PrepareBody(selector, sequence, new SelectCounterContext(sequence));

        var context = new SelectContext(buildInfo.Parent, body, sequence, buildInfo.IsSubQuery);
#if DEBUG
        context.Debug_MethodCall = methodCall;
#endif
        return ToStatementCallOr(method, context);
    }
}
