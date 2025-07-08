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
        public EntityQueryable(IAsyncQueryProvider queryProvider, Expression expression)
        {
            _queryProvider = queryProvider;
            Expression = expression;
        }
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

        public override IDbBusProvider BusProvider
             => _queryProvider;
        /// <summary>
        /// 
        /// </summary>
        public override IEnumerator<T> GetEnumerator()
            => _queryProvider.Execute<IEnumerable<T>>(Expression).GetEnumerator();




    }
}
