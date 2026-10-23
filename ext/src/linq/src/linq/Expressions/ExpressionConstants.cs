using mooSQL.data;
using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// ExpressionConstants type.
	/// </summary>
	/// <summary>
	/// ExpressionConstants type.
	/// </summary>
	public static class ExpressionConstants
	{
		/// <summary>
		/// DataContextParam
		/// </summary>
		/// <summary>
		/// DataContextParam
		/// </summary>
		public static readonly ParameterExpression DataContextParam = Expression.Parameter(typeof(DBInstance), "dctx");
	}
}
