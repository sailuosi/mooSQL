using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;
using System;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitMerge(MergeCall method) => VisitMergeCore(method, MergeBuilder.CanBuildMethod, MergeBuilder.Compile);
    public override MethodCall VisitMergeInto(MergeIntoCall method) => VisitMergeBuilder<MergeBuilder.MergeInto>(method, MergeBuilder.MergeInto.CanBuildMethod);
    public override MethodCall VisitMergeWithOutput(MergeWithOutputCall method) => VisitMergeCore(method, MergeBuilder.CanBuildMethod, MergeBuilder.Compile);
    public override MethodCall VisitMergeWithOutputInto(MergeWithOutputIntoCall method) => VisitMergeCore(method, MergeBuilder.CanBuildMethod, MergeBuilder.Compile);
    public override MethodCall VisitOn(OnCall method) => VisitMergeBuilder<MergeBuilder.On>(method, MergeBuilder.On.CanBuildMethod);
    public override MethodCall VisitOnTargetKey(OnTargetKeyCall method) => VisitMergeBuilder<MergeBuilder.On>(method, MergeBuilder.On.CanBuildMethod);
    public override MethodCall VisitUsing(UsingCall method) => VisitMergeBuilder<MergeBuilder.Using>(method, MergeBuilder.Using.CanBuildMethod);
    public override MethodCall VisitUsingTarget(UsingTargetCall method) => VisitMergeBuilder<MergeBuilder.UsingTarget>(method, MergeBuilder.UsingTarget.CanBuildMethod);
    public override MethodCall VisitInsertWhenNotMatchedAnd(InsertWhenNotMatchedAndCall method) => VisitMergeBuilder<MergeBuilder.InsertWhenNotMatched>(method, MergeBuilder.InsertWhenNotMatched.CanBuildMethod);
    public override MethodCall VisitDeleteWhenMatchedAnd(DeleteWhenMatchedAndCall method) => VisitMergeBuilder<MergeBuilder.DeleteWhenMatched>(method, MergeBuilder.DeleteWhenMatched.CanBuildMethod);
    public override MethodCall VisitDeleteWhenNotMatchedBySourceAnd(DeleteWhenNotMatchedBySourceAndCall method) => VisitMergeBuilder<MergeBuilder.DeleteWhenNotMatchedBySource>(method, MergeBuilder.DeleteWhenNotMatchedBySource.CanBuildMethod);
    public override MethodCall VisitUpdateWhenMatchedAnd(UpdateWhenMatchedAndCall method) => VisitMergeBuilder<MergeBuilder.UpdateWhenMatched>(method, MergeBuilder.UpdateWhenMatched.CanBuildMethod);
    public override MethodCall VisitUpdateWhenMatchedAndThenDelete(UpdateWhenMatchedAndThenDeleteCall method) => VisitMergeBuilder<MergeBuilder.UpdateWhenMatchedThenDelete>(method, MergeBuilder.UpdateWhenMatchedThenDelete.CanBuildMethod);
    public override MethodCall VisitUpdateWhenNotMatchedBySourceAnd(UpdateWhenNotMatchedBySourceAndCall method) => VisitMergeBuilder<MergeBuilder.UpdateWhenNotMatchedBySource>(method, MergeBuilder.UpdateWhenNotMatchedBySource.CanBuildMethod);

    MethodCall VisitMergeCore(
        MethodCall method,
        Func<MethodCallExpression, BuildInfo, ClauseSqlTranslator, bool> canBuild,
        Func<ClauseSqlTranslator, BuildInfo, BuildSequenceResult> compile)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!canBuild(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, compile(Context.Builder, buildInfo).BuildContext);
    }

    MethodCall VisitMergeBuilder<TBuilder>(
        MethodCall method,
        Func<MethodCallExpression, BuildInfo, ClauseSqlTranslator, bool> canBuild)
        where TBuilder : MethodCallBuilder, new()
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!canBuild(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, new TBuilder().BuildSequence(Context.Builder, buildInfo).BuildContext);
    }
}
