using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	[BuildsMethodCall("AsUpdatable")]
	sealed class AsUpdatableBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ClauseSqlTranslator builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return BuildSequenceResult.FromContext(builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0])));
		}
	}
}
