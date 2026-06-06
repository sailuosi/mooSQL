using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitAllAsync(AllAsyncCall method) => VisitAllAnyAsync(method);
    public override MethodCall VisitAnyAsync(AnyAsyncCall method) => VisitAllAnyAsync(method);

    public override MethodCall VisitCountAsync(CountAsyncCall method) => VisitAggregationAsync(method);
    public override MethodCall VisitLongCountAsync(LongCountAsyncCall method) => VisitAggregationAsync(method);
    public override MethodCall VisitSumAsync(SumAsyncCall method) => VisitAggregationAsync(method);
    public override MethodCall VisitMinAsync(MinAsyncCall method) => VisitAggregationAsync(method);
    public override MethodCall VisitMaxAsync(MaxAsyncCall method) => VisitAggregationAsync(method);
    public override MethodCall VisitAverageAsync(AverageAsyncCall method) => VisitAggregationAsync(method);

    public override MethodCall VisitFirstAsync(FirstAsyncCall method) => VisitFirstSingleAsync(method);
    public override MethodCall VisitFirstOrDefaultAsync(FirstOrDefaultAsyncCall method) => VisitFirstSingleAsync(method);
    public override MethodCall VisitSingleAsync(SingleAsyncCall method) => VisitFirstSingleAsync(method);
    public override MethodCall VisitSingleOrDefaultAsync(SingleOrDefaultAsyncCall method) => VisitFirstSingleAsync(method);

    public override MethodCall VisitContainsAsync(ContainsAsyncCall method) => VisitContainsAsyncCore(method);
    public override MethodCall VisitElementAtAsync(ElementAtAsyncCall method) => VisitElementAtAsyncCore(method);
    public override MethodCall VisitElementAtOrDefaultAsync(ElementAtOrDefaultAsyncCall method) => VisitElementAtAsyncCore(method);

    MethodCall VisitAllAnyAsync(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildAllAnyAsync(methodCall))
            return method;

        return VisitAllAny(method);
    }

    MethodCall VisitAggregationAsync(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        if (!CanBuildAggregationAsync(methodCall))
            return method;

        return VisitAggregation(method);
    }

    MethodCall VisitFirstSingleAsync(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildFirstSingleAsync(methodCall, buildInfo, Context.Builder))
            return method;

        return VisitFirstSingle(method);
    }

    MethodCall VisitContainsAsyncCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildContainsAsync(methodCall, Context.Builder))
            return method;

        return VisitContainsCore(method);
    }

    MethodCall VisitElementAtAsyncCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildElementAt(methodCall))
            return method;

        return VisitElementAtCore(method);
    }
}
