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
        TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);
    }
}
