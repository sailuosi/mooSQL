using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 实体查询
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EntityQueryable<T> : BaseDbBus<T>
    {

        private readonly IAsyncQueryProvider _queryProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <summary>
        /// 由表达式树构造实体查询。
        /// </summary>
        /// <param name="queryProvider">异步查询提供程序。</param>
        /// <param name="expression">表达式树根。</param>
        public EntityQueryable(IAsyncQueryProvider queryProvider, Expression expression)
        {
            _queryProvider = queryProvider;
            Expression = expression;
        }
        /// <summary>
        /// 由实体类型构造根查询（常量为当前实例占位）。
        /// </summary>
        /// <param name="queryProvider">异步查询提供程序。</param>
        /// <param name="entityType">实体类型。</param>
        public EntityQueryable(IAsyncQueryProvider queryProvider, Type entityType)
        {
            _queryProvider = queryProvider;
            Expression = System.Linq.Expressions.Expression.Constant(this);
        }
        /// <summary>
        /// 
        /// </summary>
        public override Type ElementType
            => typeof(T);

        /// <summary>
        /// 
        /// </summary>
        public override Expression Expression { get; }

        /// <summary>
        /// 
        /// </summary>
        public override IQueryProvider Provider
            => _queryProvider;

        /// <inheritdoc />
        public override IDbBusProvider BusProvider
             => _queryProvider;
        /// <summary>
        /// 
        /// </summary>
        public override IEnumerator<T> GetEnumerator()
            => _queryProvider.Execute<IEnumerable<T>>(Expression).GetEnumerator();




    }
}
