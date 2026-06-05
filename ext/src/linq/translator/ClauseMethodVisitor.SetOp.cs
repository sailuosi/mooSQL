using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitConcat(ConcatCall method) => VisitSetOp(method);
    public override MethodCall VisitUnion(UnionCall method) => VisitSetOp(method);
    public override MethodCall VisitUnionAll(UnionAllCall method) => VisitSetOp(method);
    public override MethodCall VisitExcept(ExceptCall method) => VisitSetOp(method);
    public override MethodCall VisitExceptAll(ExceptAllCall method) => VisitSetOp(method);
    public override MethodCall VisitIntersect(IntersectCall method) => VisitSetOp(method);

    MethodCall VisitSetOp(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!SetOperationBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, SetOperationBuilder.Compile(Context.Builder, buildInfo).BuildContext);
    }
}
