using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Reflection;
	using SqlQuery;
	using Common;
	using Mapping;
	using mooSQL.linq.Expressions;
    using mooSQL.data.model;

    [BuildsMethodCall("GroupJoin")]
	sealed class GroupJoinBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var outerExpression = methodCall.Arguments[0];
			var outerContextResult = builder.TryBuildSequence(new BuildInfo(buildInfo, outerExpression, buildInfo.SelectQuery));
			if (outerContextResult.BuildContext == null)
				return outerContextResult;

			var outerContext = outerContextResult.BuildContext;

			var innerExpression = methodCall.Arguments[1].Unwrap();

			var outerKeyLambda = methodCall.Arguments[2].UnwrapLambda();
			var innerKeyLambda = methodCall.Arguments[3].UnwrapLambda();
			var resultLambda   = methodCall.Arguments[4].UnwrapLambda();

			var outerKey = SequenceHelper.PrepareBody(outerKeyLambda, outerContext);

			var elementType = TypeHelper.GetEnumerableElementType(resultLambda.Parameters[1].Type);
			var innerContext = new GroupJoinInnerContext(buildInfo.Parent, outerContext.SelectQuery, builder,
				elementType,
				outerKey,
				innerKeyLambda, innerExpression);

			var resultExpression = SequenceHelper.PrepareBody(resultLambda, outerContext, innerContext);

			var context = new SelectContext(buildInfo.Parent, resultExpression, outerContext, buildInfo.IsSubQuery);

			return BuildSequenceResult.FromContext(context);
		}

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		class GroupJoinInnerContext : BuildContextBase
		{


			public GroupJoinInnerContext(
				IBuildContext? parent, SelectQueryClause outerQuery, ExpressionBuilder builder, Type elementType,
				Expression outerKey, LambdaExpression innerKeyLambda,
				Expression innerExpression
			) : base(builder, elementType, outerQuery)
			{
				Parent          = parent;
				OuterKey        = outerKey;
				InnerKeyLambda  = innerKeyLambda;
				InnerExpression = innerExpression;
			}

			Expression       OuterKey        { get; }
			LambdaExpression InnerKeyLambda  { get; }
			Expression       InnerExpression { get; }

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.HasFlag(ProjectFlags.Root) && SequenceHelper.IsSameContext(path, this))
				{
					return path;
				}

				if (SequenceHelper.IsSameContext(path, this) 
					&& (flags.IsExpression() || flags.IsExtractProjection())
				    && !path.Type.IsAssignableFrom(ElementType))
				{
					var result = GetGroupJoinCall();
					return result;
				}

				return path;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new GroupJoinInnerContext(null, context.CloneElement(SelectQuery), Builder, ElementType,
					context.CloneExpression(OuterKey), context.CloneExpression(InnerKeyLambda), context.CloneExpression(InnerExpression));
			}

			public override void SetRunQuery<T>(SentenceBag<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				var expr        = GetGroupJoinCall();
				var buildResult = Builder.TryBuildSequence(new BuildInfo(buildInfo.Parent, expr, new SelectQueryClause()));
				return buildResult.BuildContext;
			}

			public override BaseSentence GetResultStatement()
			{
				return new SelectSentence(SelectQuery);
			}

			Expression GetGroupJoinCall()
			{
				// Generating the following
				// innerExpression.Where(o => o.Key == innerKey)

				var filterLambda = Expression.Lambda(ExpressionBuilder.Equal(
						DB,
						OuterKey,
						InnerKeyLambda.Body),
					InnerKeyLambda.Parameters[0]);

				var expr = (Expression)Expression.Call(
					Methods.Queryable.Where.MakeGenericMethod(filterLambda.Parameters[0].Type),
					InnerExpression,
					filterLambda);

				expr = SequenceHelper.MoveToScopedContext(expr, this);

				return expr;
			}

		}
	}
}
