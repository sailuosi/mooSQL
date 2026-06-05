using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.ext;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitAll(AllCall method) => VisitAllAny(method);
    public override MethodCall VisitAny(AnyCall method) => VisitAllAny(method);

    MethodCall VisitAllAny(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!AllAnyBuilder.CanBuildMethod(methodCall, buildInfo, Context.Builder)
            && !AllAnyBuilder.CanBuildAsyncMethod(methodCall, buildInfo, Context.Builder))
        {
            Context.BuildResult = BuildSequenceResult.NotSupported();
            return method;
        }

        var sequenceBuildInfo = new BuildInfo(buildInfo, methodCall.Arguments[0])
        {
            CreateSubQuery = true,
            SourceCardinality = SourceCardinality.Unknown,
            SelectQuery = new SelectQueryClause()
        };

        var buildResult = Context.Builder.TryBuildSequence(sequenceBuildInfo);
        if (buildResult.BuildContext == null)
        {
            Context.BuildResult = buildResult;
            return method;
        }

        var sequence = buildResult.BuildContext;
        var isAsync = methodCall.Method.DeclaringType == typeof(AsyncExtensions);

        if (methodCall.Arguments.Count == (isAsync ? 3 : 2))
        {
            if (sequence.SelectQuery.Select.TakeValue != null ||
                sequence.SelectQuery.Select.SkipValue != null)
            {
                sequence = new SubQueryContext(sequence);
            }

            var condition = (LambdaExpression)methodCall.Arguments[1].Unwrap();

            if (methodCall.Method.Name.StartsWith("All"))
                condition = Expression.Lambda(Expression.Not(condition.Body), condition.Name, condition.Parameters);

            sequence = Context.Builder.BuildWhere(
                buildInfo.Parent, sequence,
                condition: condition, checkForSubQuery: true, enforceHaving: false,
                isTest: buildInfo.IsTest);

            if (sequence == null)
            {
                Context.BuildResult = BuildSequenceResult.Error(methodCall);
                return method;
            }

            sequence.SetAlias(condition.Parameters[0].Name);
        }

        _ = Context.Builder.MakeExpression(
            sequence,
            new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], sequence),
            ProjectFlags.ExtractProjection);

        return ToStatementCallOr(method,
            new AllAnyBuilder.AllAnyContext(buildInfo.Parent, buildInfo.SelectQuery, methodCall, sequence));
    }
}
