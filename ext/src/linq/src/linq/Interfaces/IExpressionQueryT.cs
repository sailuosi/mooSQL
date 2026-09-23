using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq
{
	public interface IExpressionQuery<out T> : IOrderedQueryable<T>, IExpressionQuery
	{
		new Expression Expression { get; }
	}
}
