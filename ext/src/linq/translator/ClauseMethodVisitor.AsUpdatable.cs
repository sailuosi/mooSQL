using System.Linq.Expressions;
using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.data.model;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitAsUpdatable(AsUpdatableCall method) => VisitAsUpdatableCore(method);
    public override MethodCall VisitAsValueInsertable(AsValueInsertableCall method) => VisitAsValueInsertableCore(method);

    MethodCall VisitAsUpdatableCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildAsUpdatable(methodCall))
            return method;

        return ToStatementCallOr(method, BuildAsUpdatableCore(Context.Builder, methodCall, buildInfo));
    }

    MethodCall VisitAsValueInsertableCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildAsUpdatable(methodCall))
            return method;

        return ToStatementCallOr(method, BuildAsValueInsertableCore(Context.Builder, methodCall, buildInfo));
    }

    static bool CanBuildAsUpdatable(MethodCallExpression call) => call.IsQueryable();

    static IBuildContext BuildAsUpdatableCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
        => builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

    static IBuildContext BuildAsValueInsertableCore(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
    {
        var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

        return new InsertContext(sequence,
            InsertContext.InsertTypeEnum.Insert, new InsertSentence(sequence.SelectQuery), null)
        {
            RequiresSetters = true
        };
    }
}
