using System.Linq.Expressions;

namespace mooSQL.linq
{
	using mooSQL.linq.mapping;
    using mooSQL.data;
    /// <summary>
    /// IExpressionInfo interface.
    /// </summary>

    public interface IExpressionInfo
	{
		/// <summary>
		/// Gets GetExpression.
		/// </summary>
		/// <summary>
		/// Gets GetExpression.
		/// </summary>
		LambdaExpression GetExpression(DBInstance mappingSchema);
	}
}
