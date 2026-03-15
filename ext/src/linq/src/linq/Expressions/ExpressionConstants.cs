using mooSQL.data;
using System.Linq.Expressions;

namespace mooSQL.linq.Expressions
{
	public static class ExpressionConstants
	{
		public static readonly ParameterExpression DataContextParam = Expression.Parameter(typeof(DBInstance), "dctx");
	}
}
