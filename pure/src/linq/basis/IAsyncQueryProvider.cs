using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 可异步的查询执行器
    /// </summary>
    public interface IAsyncQueryProvider:IDbBusProvider
    {
        /// <summary>
        /// 异步执行表达式并返回结果（由编译器实现同步或异步底层）。
        /// </summary>
        /// <typeparam name="TResult">结果类型。</typeparam>
        /// <param name="expression">表达式树。</param>
        /// <param name="cancellationToken">取消标记。</param>
        TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);
    }
}
