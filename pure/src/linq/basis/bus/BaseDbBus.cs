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
        public abstract IDbBusProvider BusProvider { get; }
        public abstract Expression Expression { get; }
        public abstract Type ElementType { get; }
        public abstract IQueryProvider Provider { get; }

        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// .LeftJoin((a,b)=>a.id==b.id)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="src"></param>
        /// <param name="onCondition"></param>
        /// <returns></returns>
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
        /// 内连接
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="src"></param>
        /// <param name="onCondition"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
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
        /// 右连接
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="src"></param>
        /// <param name="onCondition"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
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
