using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Common;

	using mooSQL.data.model;
	using mooSQL.linq.Expressions;
    using mooSQL.linq.ext;
    using SqlQuery;

	[BuildsMethodCall("Join", "InnerJoin", "LeftJoin", "RightJoin", "FullJoin", "CrossJoin")]
	sealed class AllJoinsLinqBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
		{
			if (call.Method.DeclaringType != typeof(LinqExtensions))
				return false;

			if (!call.IsQueryable())
				return false;

			return call.Arguments.Count == (call.Method.Name switch
			{
				"Join" => 5,
				"CrossJoin" => 3,
				_ => 4,
			});
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var outerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var innerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQueryClause()));

            List<QueryExtension>? extensions = null;

			var jhc = SequenceHelper.GetJoinHintContext(innerContext);
			if (jhc != null)
			{
				innerContext = jhc.Context;
				extensions   = jhc.Extensions;
			}

			JoinKind joinType;
			var conditionIndex = 2;

			switch (methodCall.Method.Name)
			{
				case "InnerJoin" : joinType = JoinKind.Inner; break;
				case "CrossJoin" : joinType = JoinKind.Inner; conditionIndex = -1; break;
				case "LeftJoin"  : joinType = JoinKind.Left;  break;
				case "RightJoin" : joinType = JoinKind.Right; break;
				case "FullJoin"  : joinType = JoinKind.Full;  break;
				default:
					conditionIndex = 3;

					joinType = (SqlJoinType) builder.EvaluateExpression(methodCall.Arguments[2])! switch
					{
						SqlJoinType.Inner => JoinKind.Inner,
						SqlJoinType.Left  => JoinKind.Left,
						SqlJoinType.Right => JoinKind.Right,
						SqlJoinType.Full  => JoinKind.Full,
						_                 => throw new InvalidOperationException($"Unexpected join type: {(SqlJoinType)builder.EvaluateExpression(methodCall.Arguments[2])!}")
					};
					break;
			}

			if (joinType == JoinKind.Right || joinType == JoinKind.Full)
				outerContext = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, outerContext, outerContext, null, false, false);
			outerContext = new SubQueryContext(outerContext);

			if (joinType == JoinKind.Left || joinType == JoinKind.Full)
				innerContext = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, innerContext, innerContext, null, false, false);
			innerContext = new SubQueryContext(innerContext);

			var selector = methodCall.Arguments[methodCall.Arguments.Count-1].UnwrapLambda();
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

			if (conditionIndex != -1)
			{
				var condition     = methodCall.Arguments[conditionIndex].UnwrapLambda();

				// Comparison should be provided without DefaultIfEmptyBuilder, so we left original contexts for comparison
				// ScopeContext ensures that comparison will placed on needed level.
				//
				var conditionExpr = SequenceHelper.PrepareBody(condition, outerContext, innerContext);

				conditionExpr = builder.ConvertExpression(conditionExpr);

				//var join  = new FromClause.Join(joinType, innerContext.SelectQuery, null, false, Array.Empty<FromClause.Join>());

				//outerContext.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
				outerContext.SelectQuery.From.Join(joinType, innerContext.SelectQuery, null, null);


				if (extensions != null)
					//join.JoinedTable.SqlQueryExtensions = extensions;

					throw new NotImplementedException();
				var flags = ProjectFlags.SQL;

				//builder.BuildSearchCondition(
				//	joinContext,
				//	conditionExpr, flags,
				//	join.JoinedTable.Condition);

				/*if (joinType == JoinType.Full)
				{
					join.JoinedTable.Condition = QueryHelper.CorrectComparisonForJoin(join.JoinedTable.Condition);
				}*/
			}
			else
			{
				outerContext.SelectQuery.From.FindTableSrc(innerContext.SelectQuery);
			}

			return BuildSequenceResult.FromContext(joinContext);
		}
	}
}
