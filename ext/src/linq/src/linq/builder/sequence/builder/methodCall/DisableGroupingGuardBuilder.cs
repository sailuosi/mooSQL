using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;
    using mooSQL.linq.ext;
    using Reflection;

	[BuildsMethodCall(nameof(LinqExtensions.DisableGuard))]
	sealed class DisableGroupingGuardBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(Methods.LinqToDB.DisableGuard);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var saveDisabledFlag = builder.IsGroupingGuardDisabled;
			builder.IsGroupingGuardDisabled = true;
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			builder.IsGroupingGuardDisabled = saveDisabledFlag;

			return sequence;
		}
	}
}
