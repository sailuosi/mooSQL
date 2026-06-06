using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;
    using mooSQL.linq.ext;
    using Reflection;

	[BuildsMethodCall(nameof(LinqExtensions.IgnoreFilters))]
	sealed class IgnoreFiltersBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
			=> call.IsSameGenericMethod(Methods.SooQuery.IgnoreFilters);

		protected override BuildSequenceResult BuildMethodCall(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var types = builder.EvaluateExpression<Type[]>(methodCall.Arguments[1])!;

			builder.PushDisabledQueryFilters(types);
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			builder.PopDisabledFilter();

			return sequence;
		}
	}
}
