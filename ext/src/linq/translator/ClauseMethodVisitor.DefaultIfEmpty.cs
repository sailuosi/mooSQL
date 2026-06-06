using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.SqlQuery;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitDefaultIfEmpty(DefaultIfEmptyCall method) => VisitDefaultIfEmptyCore(method);

    MethodCall VisitDefaultIfEmptyCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildDefaultIfEmpty(methodCall))
            return method;

        var builder = Context.Builder;
        var defaultValue = methodCall.Arguments.Count == 1 ? null : methodCall.Arguments[1].Unwrap();

        IBuildContext? result;

        if (buildInfo.SourceCardinality == SourceCardinality.Unknown)
        {
            var sequenceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQueryClause()));
            if (sequenceResult.BuildContext == null)
                return method;

            var sequence = sequenceResult.BuildContext;

            defaultValue ??= Expression.Default(methodCall.Method.GetGenericArguments()[0]);

            var defaultValueContext = new SelectContext(buildInfo.Parent,
                builder,
                null,
                defaultValue,
                new SelectQueryClause(), buildInfo.IsSubQuery);

            var subqueryContext = new SubQueryContext(defaultValueContext);

            subqueryContext.SelectQuery.From.LeftJoin(sequence.SelectQuery, "d", null);

            var defaultRef = new ContextRefExpression(defaultValueContext.ElementType, defaultValueContext);

            var translated = builder.BuildSqlExpression(defaultValueContext, defaultRef, ProjectFlags.SQL, buildFlags: BuildFlags.ForceAssignments);
            translated = builder.UpdateNesting(subqueryContext, translated);
            if (defaultValueContext.SelectQuery.Select.Columns.Count == 0)
            {
                defaultValueContext.SelectQuery.Select.AddNew(new ValueWord(1));
            }

            var defaultIfEmptyContext = new DefaultIfEmptyContext(buildInfo.Parent, sequence, sequence, null, true, false);

            var notNullConditions = defaultIfEmptyContext.GetNotNullConditions();

            var defaultIfEmptyRef = new ContextRefExpression(defaultIfEmptyContext.ElementType, defaultIfEmptyContext);
            var defaultRefExpr = (Expression)defaultRef;
            if (defaultRefExpr.Type != defaultIfEmptyRef.Type)
            {
                defaultRefExpr = Expression.Convert(defaultRefExpr, defaultIfEmptyRef.Type);
            }

            var condition = notNullConditions.Select(SequenceHelper.MakeNotNullCondition).Aggregate(Expression.AndAlso);
            var bodyValue = Expression.Condition(condition, defaultIfEmptyRef, defaultRefExpr);

            var resultSelectContext =
                new SelectContext(buildInfo.Parent, bodyValue, subqueryContext, buildInfo.IsSubQuery);

            if (!buildInfo.IsSubQuery)
            {
                if (!builder.IsSupportedSubquery(resultSelectContext, resultSelectContext, out _))
                    return method;
            }

            result = new SubQueryContext(resultSelectContext);
        }
        else
        {
            var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]) { SourceCardinality = buildInfo.SourceCardinality | SourceCardinality.Zero });
            if (buildResult.BuildContext == null)
                return method;

            var sequence = buildResult.BuildContext;

            result = new DefaultIfEmptyContext(buildInfo.Parent, sequence, sequence, defaultValue, true, false);
        }

        return ToStatementCallOr(method, result);
    }

    static bool CanBuildDefaultIfEmpty(MethodCallExpression call)
        => call.IsQueryable();
}
