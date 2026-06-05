using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

/// <summary>
/// Where 谓词扩展方法（Like/InList/IsNull 等）。序列级 Visit 委托扩展 Builder；Phase E 迁入 <see cref="ClausePredicateVisitor"/>。
/// </summary>
internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitInList(InListCall method) => VisitPredicateExtension(method);
    public override MethodCall VisitLike(LikeCall method) => VisitPredicateExtension(method);
    public override MethodCall VisitLikeLeft(LikeLeftCall method) => VisitPredicateExtension(method);
    public override MethodCall VisitIsNull(IsNullCall method) => VisitPredicateExtension(method);
    public override MethodCall VisitIsNotNull(IsNotNullCall method) => VisitPredicateExtension(method);
    public override MethodCall VisitStartsWith(StartsWithCall method) => VisitPredicateExtension(method);
    public override MethodCall VisitEquals(EqualsCall method) => VisitPredicateExtension(method);

    MethodCall VisitPredicateExtension(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression mc)
            return method;

        var buildInfo = Context.CreateBuildInfo(mc);
        if (QueryExtensionBuilder.CanBuild(mc, buildInfo, Context.Builder))
        {
            Context.BuildResult = new QueryExtensionBuilder().BuildSequence(Context.Builder, buildInfo);
            return method;
        }

        if (MethodChainBuilder.CanBuild(mc, buildInfo, Context.Builder))
        {
            Context.BuildResult = new MethodChainBuilder().BuildSequence(Context.Builder, buildInfo);
            return method;
        }

        Context.BuildResult = BuildSequenceResult.NotSupported();
        return method;
    }
}
