using mooSQL.data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq
{
	/// <summary>
	/// IExpressionQuery type.
	/// </summary>
	/// <summary>
	/// IExpressionQuery interface.
	/// </summary>
	public interface IExpressionQuery : IQueryProvider
	{
		/// <summary>
		/// Expression
		/// </summary>
		/// <summary>
		/// Gets or sets Expression.
		/// </summary>
		Expression   Expression  { get; }
		/// <summary>
		/// SqlText
		/// </summary>
		/// <summary>
		/// Gets or sets SqlText.
		/// </summary>
		string       SqlText     { get; }
		/// <summary>
		/// DBLive
		/// </summary>
		/// <summary>
		/// Gets or sets DBLive.
		/// </summary>
		DBInstance DBLive { get; }
		/// <summary>
		/// ExecuteAsync&lt;TResult&gt;
		/// </summary>

		Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);

#if NET6_0_OR_GREATER
		/// <summary>
		/// ExecuteAsyncEnumerable&lt;TResult&gt;
		/// </summary>
		/// <summary>
		/// ExecuteAsyncEnumerable&lt;TResult&gt;
		/// </summary>
		Task<IAsyncEnumerable<TResult>> ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken = default);
#endif
	}
}
