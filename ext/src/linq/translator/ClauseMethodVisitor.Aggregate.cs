using mooSQL.data.call;
using mooSQL.linq.Linq.Builder;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitCount(CountCall method) => VisitAggregation(method);
    public override MethodCall VisitLongCount(LongCountCall method) => VisitAggregation(method);
    public override MethodCall VisitSum(SumCall method) => VisitAggregation(method);
    public override MethodCall VisitMin(MinCall method) => VisitAggregation(method);
    public override MethodCall VisitMax(MaxCall method) => VisitAggregation(method);
    public override MethodCall VisitAverage(AverageCall method) => VisitAggregation(method);

    MethodCall VisitAggregation(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!AggregationBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder)
            && !AggregationBuilder.CanBuildAsyncMethod(methodCall, buildInfo, Context.Builder))
        {
            Context.BuildResult = BuildSequenceResult.NotSupported();
            return method;
        }

        Context.BuildResult = AggregationBuilder.Compile(Context.Builder, buildInfo);
        return method;
    }
}
