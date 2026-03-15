using System;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;
	using SqlQuery;
	using Common;

	using static mooSQL.linq.Reflection.Methods.LinqToDB.Merge;
    using mooSQL.data.model;
    using mooSQL.linq.ext;

    internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.Merge))]
		internal sealed class Merge : MethodCallBuilder
		{
			static readonly MethodInfo[] _supportedMethods = { MergeMethodInfo1, MergeMethodInfo2 };

			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsSameGenericMethod(_supportedMethods);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// Merge(ITable<TTarget> target, string hint)

				var disableFilters = methodCall.Arguments[0] is not MethodCallExpression mc || mc.Method.Name != nameof(LinqExtensions.AsCte);
				if (disableFilters)
					builder.PushDisabledQueryFilters(new Type[] { });

				var target = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQueryClause()) { AssociationsAsSubQueries = true });

				if (disableFilters)
					builder.PopDisabledFilter();

				var targetTable = GetTargetTable(target);

				if (targetTable == null)
					throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() before calling .Merge()");

				var merge = new MergeSentence(targetTable);
				if (methodCall.Arguments.Count == 2)
					merge.Hint = builder.EvaluateExpression<string>(methodCall.Arguments[1]);

				target.SetAlias(merge.Target.FindAlias()!);

				return BuildSequenceResult.FromContext(new MergeContext(merge, target));
			}
		}
	}
}
