using System;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data.model;
    using mooSQL.linq.Expressions;
    using mooSQL.linq.ext;
    using SqlQuery;

	using static mooSQL.linq.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.MergeInto))]
		internal sealed class MergeInto : MethodCallBuilder
		{
			static readonly MethodInfo[] _supportedMethods = { MergeIntoMethodInfo1, MergeIntoMethodInfo2 };

			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsSameGenericMethod(_supportedMethods);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// MergeInto<TTarget, TSource>(IQueryable<TSource> source, ITable<TTarget> target, string hint)
				var sourceContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQueryClause()));
				var target        = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1]) { AssociationsAsSubQueries = true });

				var targetTable = GetTargetTable(target);
				if (targetTable == null)
					throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() on the parameter before passing into .MergeInto().");

				var merge = new MergeSentence(targetTable);
				if (methodCall.Arguments.Count == 3)
					merge.Hint = builder.EvaluateExpression<string>(methodCall.Arguments[2]);

				target.SetAlias(merge.Target.FindAlias()!);

				var genericArguments = methodCall.Method.GetGenericArguments();

				var source = new TableLikeQueryContext(new ContextRefExpression(genericArguments[0], target, "t"),
					new ContextRefExpression(genericArguments[1], sourceContext, "s"));

				return BuildSequenceResult.FromContext(new MergeContext(merge, target, source));
			}

		}
	}
}
