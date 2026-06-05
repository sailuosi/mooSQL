using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitWhere(WhereCall method) => VisitWhereHaving(method, isHaving: false);

    public override MethodCall VisitHaving(HavingCall method) => VisitWhereHaving(method, isHaving: true);

    MethodCall VisitWhereHaving(MethodCall method, bool isHaving)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!WhereBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder))
        {
            Context.BuildResult = BuildSequenceResult.NotSupported();
            return method;
        }

        var sequenceResult = Context.Builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        if (sequenceResult.BuildContext == null)
        {
            Context.BuildResult = sequenceResult;
            return method;
        }

        var sequence = sequenceResult.BuildContext;
        var condition = methodCall.Arguments[1].UnwrapLambda();

        if (sequence.SelectQuery.Select.IsDistinct
            || sequence.SelectQuery.Select.TakeValue != null
            || sequence.SelectQuery.Select.SkipValue != null)
        {
            sequence = new SubQueryContext(sequence);
        }

        var result = Context.Builder.BuildWhere(
            buildInfo.Parent, sequence, condition: condition,
            checkForSubQuery: !isHaving, enforceHaving: isHaving, isTest: buildInfo.IsTest);

        if (result == null)
        {
            Context.BuildResult = BuildSequenceResult.Error(methodCall);
            return method;
        }

        result.SetAlias(condition.Parameters[0].Name);
        Context.BuildResult = BuildSequenceResult.FromContext(result);
        return method;
    }
}
