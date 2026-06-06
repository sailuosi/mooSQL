using System.Linq;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;
using mooSQL.utils;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitAsCte(AsCteCall method) => VisitAsCteCore(method);
    public override MethodCall VisitAsQueryable(AsQueryableCall method) => DispatchPassThrough(method);
    public override MethodCall VisitFromSql(FromSqlCall method) => VisitFromSqlCore(method);
    public override MethodCall VisitFromSqlScalar(FromSqlScalarCall method) => VisitFromSqlScalarCore(method);
    public override MethodCall VisitGetCte(GetCteCall method) => VisitGetCteCore(method);
    public override MethodCall VisitUseQueryable(UseQueryableCall method) => VisitUseQueryableCore(method);
    public override MethodCall VisitTableFromExpression(TableFromExpressionCall method) => VisitTableFromExpressionCore(method);

    MethodCall VisitAsCteCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildKnownTableMethods(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, ClauseExpressionVisitor.BuildAsCteCore(Context.Builder, buildInfo));
    }

    MethodCall VisitFromSqlCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildKnownTableMethods(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, ClauseExpressionVisitor.BuildFromSqlCore(Context.Builder, buildInfo));
    }

    MethodCall VisitFromSqlScalarCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildKnownTableMethods(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, ClauseExpressionVisitor.BuildFromSqlScalarCore(Context.Builder, buildInfo));
    }

    MethodCall VisitGetCteCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildKnownTableMethods(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, ClauseExpressionVisitor.BuildGetCteCore(Context.Builder, buildInfo));
    }

    MethodCall VisitUseQueryableCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildTableMethods(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, ClauseExpressionVisitor.BuildUseQueryableCore(Context.Builder, buildInfo));
    }

    MethodCall VisitTableFromExpressionCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildTableMethods(methodCall, buildInfo, Context.Builder))
            return method;

        return ToStatementCallOr(method, ClauseExpressionVisitor.BuildTableFromExpressionCore(Context.Builder, buildInfo));
    }

    static bool CanBuildKnownTableMethods(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
        => true;

    static bool CanBuildTableMethods(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
        => typeof(IDbQuery<>).IsSameOrParentOf(call.Type);
}
