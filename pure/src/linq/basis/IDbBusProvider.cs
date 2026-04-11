using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 扩展 <see cref="IQueryProvider"/>，支持创建 mooSQL 查询总线 <see cref="IDbBus{T}"/>。
    /// </summary>
    public interface IDbBusProvider: IQueryProvider
    {
        /// <summary>
        /// 由表达式创建强类型查询总线。
        /// </summary>
        IDbBus<TElement> CreateBus<TElement>(Expression expression);
    }
}
