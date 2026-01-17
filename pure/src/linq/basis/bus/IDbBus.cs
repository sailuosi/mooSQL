using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 暴露用户侧的 query接口，顶级接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDbBus<T>:IQueryable<T>, IOrderedQueryable<T>
    {
        /// <summary>
        /// 自定义的提供器
        /// </summary>
        IDbBusProvider BusProvider
        {
            get;
        }


        #region 扩展的linq方法
        IDbBus<T> LeftJoin<E>(Expression<Func<T, E, bool>> onCondition);

        IDbBus<T> InnerJoin<E>(Expression<Func<T, E, bool>> onCondition);

        IDbBus<T> RightJoin<E>(Expression<Func<T, E, bool>> onCondition);
        #endregion
    }
}
