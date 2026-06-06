using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Common;
using mooSQL.linq.Mapping;
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
        if (!CanBuildGroupJoin(methodCall))
            return method;

        var builder = Context.Builder;
        var outerContextResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], buildInfo.SelectQuery));
        if (outerContextResult.BuildContext == null)
            return method;

        var outerContext = outerContextResult.BuildContext;
        var innerExpression = methodCall.Arguments[1].Unwrap();
        var outerKeyLambda = methodCall.Arguments[2].UnwrapLambda();
        var innerKeyLambda = methodCall.Arguments[3].UnwrapLambda();
        var resultLambda   = methodCall.Arguments[4].UnwrapLambda();
        var outerKey = SequenceHelper.PrepareBody(outerKeyLambda, outerContext);

        var elementType = TypeHelper.GetEnumerableElementType(resultLambda.Parameters[1].Type);
        var innerContext = new GroupJoinInnerContext(buildInfo.Parent, outerContext.SelectQuery, builder,
            elementType, outerKey, innerKeyLambda, innerExpression);

        var resultExpression = SequenceHelper.PrepareBody(resultLambda, outerContext, innerContext);
        var context = new SelectContext(buildInfo.Parent, resultExpression, outerContext, buildInfo.IsSubQuery);

        return ToStatementCallOr(method, context);
    }

    static bool CanBuildGroupJoin(MethodCallExpression call)
        => call.IsQueryable();
}
