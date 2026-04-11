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
        /// <summary>左连接，条件为两表字段关系。</summary>
        /// <typeparam name="E">右表实体类型。</typeparam>
        IDbBus<T> LeftJoin<E>(Expression<Func<T, E, bool>> onCondition);

        /// <summary>内连接。</summary>
        /// <typeparam name="E">右表实体类型。</typeparam>
        IDbBus<T> InnerJoin<E>(Expression<Func<T, E, bool>> onCondition);

        /// <summary>右连接。</summary>
        /// <typeparam name="E">右表实体类型。</typeparam>
        IDbBus<T> RightJoin<E>(Expression<Func<T, E, bool>> onCondition);
        #endregion
    }
}
