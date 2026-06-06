using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	static class WhereBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
			=> call.IsQueryable();
	}
}
