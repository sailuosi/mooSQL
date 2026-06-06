using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	static class SelectBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
		{
			if (!call.IsQueryable())
				return false;

			var lambda = (LambdaExpression)call.Arguments[1].Unwrap();
			return lambda.Parameters.Count is 1 or 2;
		}
	}
}
