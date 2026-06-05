using mooSQL.data.call;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq.Builder;
using System;
using System.Linq.Expressions;

namespace mooSQL.linq.translator;

internal partial class ClauseMethodVisitor
{
    public override MethodCall VisitJoin(JoinCall method) => VisitJoinCore(method, AllJoinsBuilder.CanBuildJoin);
    public override MethodCall VisitInnerJoin(InnerJoinCall method) => VisitJoinCore(method, AllJoinsBuilder.CanBuildMethod);
    public override MethodCall VisitLeftJoin(LeftJoinCall method) => VisitJoinCore(method, AllJoinsBuilder.CanBuildMethod);
    public override MethodCall VisitRightJoin(RightJoinCall method) => VisitJoinCore(method, AllJoinsBuilder.CanBuildMethod);
    public override MethodCall VisitFullJoin(FullJoinCall method) => VisitJoinCore(method, AllJoinsBuilder.CanBuildMethod);

    MethodCall VisitJoinCore(
        MethodCall method,
        Func<MethodCallExpression, BuildInfo, ExpressionBuilder, bool> canBuild)
    {
        if (method.callExpression is not MethodCallExpression methodCall)
            return method;

        var buildInfo = Context.CreateBuildInfo(methodCall);
        if (!canBuild(methodCall, buildInfo, Context.Builder))
        {
            Context.BuildResult = BuildSequenceResult.NotSupported();
            return method;
        }

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
            {
                Context.BuildResult = BuildSequenceResult.Error(methodCall);
                return method;
            }

            result.SetAlias(condition.Parameters[0].Name);
        }

        if (joinType is JoinKind.Left or JoinKind.Full)
        {
            result = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent,
                sequence: result,
                nullabilitySequence: result,
                defaultValue: null,
                allowNullField: false,
                isNullValidationDisabled: false);
        }

        Context.BuildResult = BuildSequenceResult.FromContext(result);
        return method;
    }
}
