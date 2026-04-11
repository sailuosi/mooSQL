using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 基础的查询实现，对应于接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseDbBus<T> : IDbBus<T>
    {
        /// <summary>
        /// 用于创建子查询/连接等操作的总线提供者。
        /// </summary>
        public abstract IDbBusProvider BusProvider { get; }
        /// <summary>
        /// 描述当前总线所表示查询的表达式树。
        /// </summary>
        public abstract Expression Expression { get; }
        /// <summary>
        /// 序列元素类型（实体或投影类型）。
        /// </summary>
        public abstract Type ElementType { get; }
        /// <summary>
        /// 关联的 LINQ 查询提供程序。
        /// </summary>
        public abstract IQueryProvider Provider { get; }

        /// <summary>
        /// 返回用于枚举查询结果的迭代器。
        /// </summary>
        /// <returns>元素类型为 <typeparamref name="T"/> 的枚举器。</returns>
        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 左连接：.LeftJoin((a,b)=&gt;a.id==b.id)
        /// </summary>
        /// <typeparam name="E">右表实体类型。</typeparam>
        /// <param name="onCondition">连接条件。</param>
        /// <returns>新的查询总线。</returns>
        public IDbBus<T> LeftJoin<E>( Expression<Func<T, E, bool>> onCondition)
        {

            if (onCondition == null) throw new ArgumentNullException(nameof(onCondition));
            return this.BusProvider.CreateBus<T>(
                Expression.Call(
                    Expression.Constant(this),
                    new Func<Expression<Func<T, E, bool>>, IDbBus<T>>(LeftJoin).Method,
                    Expression.Quote(onCondition)
                    ));
        }
        /// <summary>
        /// 内连接。
        /// </summary>
        /// <typeparam name="E">右表实体类型。</typeparam>
        /// <param name="onCondition">连接条件。</param>
        /// <returns>新的查询总线。</returns>
        /// <exception cref="ArgumentNullException">条件为 null 时抛出。</exception>
        public IDbBus<T> InnerJoin<E>( Expression<Func<T, E, bool>> onCondition)
        {

            if (onCondition == null) throw new ArgumentNullException(nameof(onCondition));

            return this.BusProvider.CreateBus<T>(
                Expression.Call(
                    Expression.Constant(this),
                    new Func<Expression<Func<T, E, bool>>, IDbBus<T>>(InnerJoin).Method,
                    Expression, Expression.Quote(onCondition)
                    ));
        }
        /// <summary>
        /// 右连接。
        /// </summary>
        /// <typeparam name="E">右表实体类型。</typeparam>
        /// <param name="onCondition">连接条件。</param>
        /// <returns>新的查询总线。</returns>
        /// <exception cref="ArgumentNullException">条件为 null 时抛出。</exception>
        public IDbBus<T> RightJoin<E>( Expression<Func<T, E, bool>> onCondition)
        {
            var t = new Func<Expression<Func<T, E, bool>>, IDbBus<T>>(RightJoin);

            return this.BusProvider.CreateBus<T>(
                Expression.Call(
                    Expression.Constant(this),
                    t.Method,
                    Expression, Expression.Quote(onCondition)
                    ));
        }
    }
}
