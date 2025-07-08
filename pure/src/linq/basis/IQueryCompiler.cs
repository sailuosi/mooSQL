using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 查询编译器
    /// </summary>
    public interface IQueryCompiler
    {
        /// <summary>
        /// 
        /// </summary>
        TResult Execute<TResult>(Expression query);

        /// <summary>
        /// 
        /// </summary>
        TResult ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        Func<QueryContext, TResult> CreateCompiledQuery<TResult>(Expression query);

        /// <summary>
        /// 
        /// </summary>
        Func<QueryContext, TResult> CreateCompiledAsyncQuery<TResult>(Expression query);
    }
}
