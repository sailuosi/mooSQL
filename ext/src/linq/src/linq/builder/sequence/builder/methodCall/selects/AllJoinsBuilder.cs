using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	static class AllJoinsBuilder
	{
		public static bool CanBuildJoin(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
			=> call.IsQueryable() && call.Arguments.Count == 3;

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
			=> call.IsQueryable() && call.Arguments.Count == 2;
	}
}
