using System;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	static class OrderByBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ClauseSqlTranslator builder)
		{
			if (!call.IsQueryable())
				return false;

			var body = call.Arguments[1].UnwrapLambda().Body.Unwrap();
			if (body.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				if (mi.NewExpression.Arguments.Count > 0 ||
				    mi.Bindings.Count == 0 ||
				    mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment))
				{
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in order by is not allowed.");
				}
			}

			return true;
		}
	}
}
