using System;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
	using Extensions;
	using mooSQL.linq.Expressions;
    using mooSQL.linq.ext;
    using mooSQL.linq.SqlQuery;
    using static mooSQL.linq.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.On), nameof(LinqExtensions.OnTargetKey))]
		internal sealed class On : MethodCallBuilder
		{
			static readonly MethodInfo[] _supportedMethods = { OnMethodInfo1, OnMethodInfo2, OnTargetKeyMethodInfo };

			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsSameGenericMethod(_supportedMethods);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;

				if (methodCall.Arguments.Count == 2)
				{
					// On<TTarget, TSource>(IMergeableOn<TTarget, TSource> merge, Expression<Func<TTarget, TSource, bool>> matchCondition)
					var predicate = methodCall.Arguments[1];
					var condition = predicate.UnwrapLambda();

					mergeContext.SourceContext.ConnectionLambda       = condition;

					// correct aliases for better error handling
					//
					mergeContext.SourceContext.TargetContextRef.Alias = condition.Parameters[0].Name;
					mergeContext.SourceContext.SourceContextRef.Alias = condition.Parameters[1].Name;

					var preparedCondition = mergeContext.SourceContext.GenerateCondition();

					BuildMatchCondition(builder, preparedCondition, mergeContext.SourceContext, statement.On);
				}
				else if (methodCall.Arguments.Count == 3)
				{
					var targetKeyLambda = methodCall.Arguments[1].UnwrapLambda();
					var sourceKeyLambda = methodCall.Arguments[2].UnwrapLambda();

					var targetKeySelector = mergeContext.SourceContext.PrepareTargetLambda(targetKeyLambda);
					var sourceKeySelector = mergeContext.SourceContext.PrepareSourceBody(sourceKeyLambda);

					mergeContext.SourceContext.TargetKeySelector = targetKeySelector;
					mergeContext.SourceContext.SourceKeySelector = sourceKeySelector;

					BuildMatchCondition(builder, targetKeySelector, sourceKeySelector, mergeContext.SourceContext, statement.On);
				}
				else
				{
					// OnTargetKey<TTarget>(IMergeableOn<TTarget, TTarget> merge)
					//

					var targetType       = statement.Target.FindSystemType()!;
					var targetLambdaType = mergeContext.SourceContext.TargetContextRef.Type;
					var pTarget          = Expression.Parameter(targetLambdaType, "t");
					var pSource          = Expression.Parameter(targetLambdaType, "s");

					var en = mergeContext.DB.client.EntityCash.getEntityInfo(targetType);

					Expression? ex = null;

					for (var i = 0; i< en.Columns.Count; i++)
					{
						var column = en.Columns[i];
						if (!column.IsPrimarykey)
							continue;

						var member = targetLambdaType.GetMemberEx(column.PropertyInfo);
						if (member == null)
							throw new InvalidOperationException($"Member '{column.PropertyInfo.Name}' is not defined in '{pTarget.Name}'");

						var expr = Expression.Equal(
							Expression.MakeMemberAccess(pTarget, member),
							Expression.MakeMemberAccess(pSource, member));
						ex = ex != null ? Expression.AndAlso(ex, expr) : expr;
					}

					if (ex == null)
						throw new LinqToDBException("Method OnTargetKey() needs at least one primary key column");

					var condition = Expression.Lambda(ex, pTarget, pSource);

					mergeContext.SourceContext.ConnectionLambda = condition;

					var generatedCondition = mergeContext.SourceContext.GenerateCondition();

					BuildMatchCondition(builder, generatedCondition, mergeContext.SourceContext, statement.On);
				}

				return BuildSequenceResult.FromContext(mergeContext);
			}
		}
	}
}
