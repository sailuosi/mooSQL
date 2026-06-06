using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.ext;
using mooSQL.data.model;
using mooSQL.linq.SqlQuery;
using System;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitJoin(JoinCall method) => VisitJoinCore(method, CanBuildJoin);
    public override MethodCall VisitInnerJoin(InnerJoinCall method) => VisitJoinCore(method, CanBuildTwoArgJoin);
    public override MethodCall VisitLeftJoin(LeftJoinCall method) => VisitJoinCore(method, CanBuildTwoArgJoin);
    public override MethodCall VisitRightJoin(RightJoinCall method) => VisitJoinCore(method, CanBuildTwoArgJoin);
    public override MethodCall VisitFullJoin(FullJoinCall method) => VisitJoinCore(method, CanBuildTwoArgJoin);
    public override MethodCall VisitCrossJoin(CrossJoinCall method) => VisitCrossJoinCore(method);

    MethodCall VisitJoinCore(
        MethodCall method,
        Func<MethodCallExpression, BuildInfo, ClauseSqlTranslator, bool> canBuild)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!canBuild(methodCall, buildInfo, Context.Builder))
            return method;

        var argument = methodCall.Arguments[0];
        if (buildInfo.Parent != null)
            argument = SequenceHelper.MoveToScopedContext(argument, buildInfo.Parent);

        var sequence = Context.Builder.BuildSequence(new BuildInfo(buildInfo, argument));

        JoinKind joinType;
        var conditionIndex = 1;

        switch (methodCall.Method.Name)
        {
            case "InnerJoin": joinType = JoinKind.Inner; break;
            case "LeftJoin": joinType = JoinKind.Left; break;
            case "RightJoin": joinType = JoinKind.Right; break;
            case "FullJoin": joinType = JoinKind.Full; break;
            default:
                conditionIndex = 2;
                joinType = (SqlJoinType)Context.Builder.EvaluateExpression(methodCall.Arguments[1])! switch
                {
                    SqlJoinType.Inner => JoinKind.Inner,
                    SqlJoinType.Left => JoinKind.Left,
                    SqlJoinType.Right => JoinKind.Right,
                    SqlJoinType.Full => JoinKind.Full,
                    _ => throw new InvalidOperationException(
                        $"Unexpected join type: {(SqlJoinType)Context.Builder.EvaluateExpression(methodCall.Arguments[1])!}")
                };
                break;
        }

        buildInfo.JoinType = joinType;
        sequence = new SubQueryContext(sequence);
        var result = sequence;

        if (methodCall.Arguments[conditionIndex] != null)
        {
            var condition = (LambdaExpression)methodCall.Arguments[conditionIndex].Unwrap();

            result = Context.Builder.BuildWhere(result, result,
                condition: condition, checkForSubQuery: false, enforceHaving: false,
                isTest: buildInfo.IsTest);

            if (result == null)
                return method;

            result.SetAlias(condition.Parameters[0].Name);
        }

        if (joinType is JoinKind.Left or JoinKind.Full)
        {
            result = new DefaultIfEmptyContext(buildInfo.Parent,
                sequence: result,
                nullabilitySequence: result,
                defaultValue: null,
                allowNullField: false,
                isNullValidationDisabled: false);
        }

        return ToStatementCallOr(method, result);
    }

    MethodCall VisitCrossJoinCore(MethodCall method)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!CanBuildCrossJoin(methodCall))
            return method;

        var builder = Context.Builder;
        var outerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
        var innerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQueryClause()));
        outerContext = new SubQueryContext(outerContext);
        innerContext = new SubQueryContext(innerContext);

        var selector = methodCall.Arguments[2].UnwrapLambda();
        var selectorBody = SequenceHelper.PrepareBody(selector, outerContext, new ScopeContext(innerContext, outerContext));

        outerContext.SetAlias(selector.Parameters[0].Name);
        innerContext.SetAlias(selector.Parameters[1].Name);

        var joinContext = new SelectContext(buildInfo.Parent, builder, null, selectorBody, outerContext.SelectQuery, buildInfo.IsSubQuery)
#if DEBUG
        {
            Debug_MethodCall = methodCall
        }
#endif
        ;

        outerContext.SelectQuery.From.FindTableSrc(innerContext.SelectQuery);
        return ToStatementCallOr(method, joinContext);
    }

    static bool CanBuildJoin(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
        => call.IsQueryable() && call.Arguments.Count == 3;

    static bool CanBuildTwoArgJoin(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
        => call.IsQueryable() && call.Arguments.Count == 2;

    static bool CanBuildCrossJoin(MethodCallExpression call)
        => call.Method.DeclaringType == typeof(LinqExtensions)
           && call.IsQueryable()
           && call.Arguments.Count == 3;
}
