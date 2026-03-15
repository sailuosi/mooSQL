using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq.Async
{
	/// <summary>
	/// 这是内部API，不应由业务侧使用.
	/// </summary>
	public interface IQueryProviderAsync : IQueryProvider
    {
#if NET6_0_OR_GREATER
        /// <summary>
        /// 这是内部API，不应由业务侧使用.
        /// </summary>
        Task<IAsyncEnumerable<TResult>> ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken);
#else

#endif


		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// </summary>
		Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken);
	}
}
