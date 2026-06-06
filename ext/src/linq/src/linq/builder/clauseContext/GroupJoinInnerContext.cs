using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using mooSQL.linq.Expressions;
	using Reflection;

	[DebuggerDisplay("{ClauseContextDebuggingHelper.GetContextInfo(this)}")]
	sealed class GroupJoinInnerContext : ClauseContextBase
	{
		public GroupJoinInnerContext(
			IClauseContext? parent, SelectQueryClause outerQuery, ClauseSqlTranslator builder, Type elementType,
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

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			if (flags.HasFlag(ProjectFlags.Root) && SequenceHelper.IsSameContext(path, this))
				return path;

			if (SequenceHelper.IsSameContext(path, this)
			    && (flags.IsExpression() || flags.IsExtractProjection())
			    && !path.Type.IsAssignableFrom(ElementType))
			{
				return GetGroupJoinCall();
			}

			return path;
		}

		public override IClauseContext Clone(CloningContext context)
		{
			return new GroupJoinInnerContext(null, context.CloneElement(SelectQuery), Builder, ElementType,
				context.CloneExpression(OuterKey), context.CloneExpression(InnerKeyLambda), context.CloneExpression(InnerExpression));
		}

		public override IClauseContext? GetContext(Expression expression, BuildInfo buildInfo)
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
			var filterLambda = Expression.Lambda(ClauseSqlTranslator.Equal(
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
