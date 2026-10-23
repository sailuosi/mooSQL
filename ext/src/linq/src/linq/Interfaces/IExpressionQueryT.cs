using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq
{
	/// <summary>
	/// IExpressionQueryT type.
	/// </summary>
	/// <summary>
	/// IExpressionQueryT interface.
	/// </summary>
	public interface IExpressionQuery<out T> : IOrderedQueryable<T>, IExpressionQuery
	{
		/// <summary>
		/// Expression
		/// </summary>
		/// <summary>
		/// Gets or sets Expression.
		/// </summary>
		new Expression Expression { get; }
	}
}
