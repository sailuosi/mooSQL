using mooSQL.data.call;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitInsert(InsertCall method) => VisitDml(method, InsertBuilder.CanBuildMethod, InsertBuilder.Compile);

    public override MethodCall VisitUpdate(UpdateCall method) => VisitDml(method, UpdateBuilder.CanBuildMethod, UpdateBuilder.Compile);

    public override MethodCall VisitDelete(DeleteCall method) => VisitDml(method, DeleteBuilder.CanBuildMethod, DeleteBuilder.Compile);

    public override MethodCall VisitInsertOrUpdate(InsertOrUpdateCall method)
        => VisitDml(method, InsertOrUpdateBuilder.CanBuildMethod, InsertOrUpdateBuilder.Compile);

    delegate BuildSequenceResult DmlCompileDelegate(ClauseSqlTranslator builder, BuildInfo buildInfo);

    MethodCall VisitDml(
        MethodCall method,
        System.Func<MethodCallExpression, BuildInfo, ClauseSqlTranslator, bool> canBuild,
        DmlCompileDelegate compile)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!canBuild(methodCall, buildInfo, Context.Builder))
        {
            Context.BuildResult = BuildSequenceResult.NotSupported();
            return method;
        }

        Context.BuildResult = compile(Context.Builder, buildInfo);
        return method;
    }
}
