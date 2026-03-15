using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using mooSQL.linq.Expressions;
	using SqlQuery;

	[BuildsMethodCall("InnerJoin", "LeftJoin", "RightJoin", "FullJoin")]
	[BuildsMethodCall("Join", CanBuildName = nameof(CanBuildJoin))]
	sealed class AllJoinsBuilder : MethodCallBuilder
	{
		public static bool CanBuildJoin(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable() && call.Arguments.Count == 3;

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable() && call.Arguments.Count == 2;

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var argument = methodCall.Arguments[0];
			if (buildInfo.Parent != null)
			{
				argument = SequenceHelper.MoveToScopedContext(argument, buildInfo.Parent);
			}

			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, argument));

			JoinKind joinType;
			var conditionIndex = 1;

			switch (methodCall.Method.Name)
			{
				case "InnerJoin" : joinType = JoinKind.Inner; break;
				case "LeftJoin"  : joinType = JoinKind.Left;  break;
				case "RightJoin" : joinType = JoinKind.Right; break;
				case "FullJoin"  : joinType = JoinKind.Full;  break;
				default:
					conditionIndex = 2;

					joinType = (SqlJoinType) builder.EvaluateExpression(methodCall.Arguments[1])! switch
					{
						SqlJoinType.Inner => JoinKind.Inner,
						SqlJoinType.Left  => JoinKind.Left,
						SqlJoinType.Right => JoinKind.Right,
						SqlJoinType.Full  => JoinKind.Full,
						_                 => throw new InvalidOperationException($"Unexpected join type: {(SqlJoinType)builder.EvaluateExpression(methodCall.Arguments[1])!}")
					};
					break;
			}

			buildInfo.JoinType = joinType;

			sequence = new SubQueryContext(sequence);
			var result = sequence;

			if (methodCall.Arguments[conditionIndex] != null)
			{
				var condition = (LambdaExpression)methodCall.Arguments[conditionIndex].Unwrap();

				result = builder.BuildWhere(result, result,
					condition : condition, checkForSubQuery : false, enforceHaving : false,
					isTest : buildInfo.IsTest);

				if (result == null)
					return BuildSequenceResult.Error(methodCall);

				/*if (joinType == JoinType.Full)
				{
					result.SelectQuery.Where.SearchCondition =
						QueryHelper.CorrectComparisonForJoin(result.SelectQuery.Where.SearchCondition);
				}*/

				result.SetAlias(condition.Parameters[0].Name);
			}

			if (joinType is JoinKind.Left or JoinKind.Full)
			{
				result = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent,
					sequence : result,
					nullabilitySequence : result,
					defaultValue : null,
					allowNullField : false,
					isNullValidationDisabled : false);
			}

			return BuildSequenceResult.FromContext(result);
		}
	}
}
