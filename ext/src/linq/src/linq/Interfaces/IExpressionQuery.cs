using mooSQL.data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
	public interface IExpressionQuery : IQueryProvider
	{
		Expression   Expression  { get; }
		string       SqlText     { get; }
		DBInstance DBLive { get; }

		Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);

#if NET6_0_OR_GREATER
		Task<IAsyncEnumerable<TResult>> ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken = default);
#endif
	}
}
