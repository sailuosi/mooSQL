using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.Extensions;

namespace mooSQL.linq
{
    /// <summary>
    /// 实体查询提供器
    /// </summary>
    public class EntityQueryProvider : IAsyncQueryProvider
    {
        //发动机
        protected readonly IQueryCompiler _queryCompiler;

        //缓存引用1
        private static MethodInfo? _genericCreateQueryMethod;
        //缓存引用2
        private MethodInfo? _genericExecuteMethod;

        public EntityQueryProvider(IQueryCompiler queryCompiler)
        {
            _queryCompiler = queryCompiler;
        }
#if NET6_0_OR_GREATER
        private static MethodInfo GenericCreateQueryMethod
            => _genericCreateQueryMethod ??= typeof(EntityQueryProvider)
            .GetMethod("CreateQuery", 1, BindingFlags.Instance | BindingFlags.Public, null, [typeof(Expression)], null)!;

        private MethodInfo GenericExecuteMethod
            => _genericExecuteMethod ??= _queryCompiler.GetType()
                .GetMethod("Execute", 1, BindingFlags.Instance | BindingFlags.Public, null, [typeof(Expression)], null)!;
#else
        private static MethodInfo GenericCreateQueryMethod
            => _genericCreateQueryMethod ??= typeof(EntityQueryProvider)
            .GetMethod("CreateQuery")!;

        private MethodInfo GenericExecuteMethod
            => _genericExecuteMethod ??= _queryCompiler.GetType()
                .GetMethod("Execute")!;
#endif





        public IQueryable CreateQuery(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            var elementType = expression.Type.GetItemType() ?? expression.Type;

            return (IQueryable)GenericCreateQueryMethod
            .MakeGenericMethod(elementType)
            .Invoke(this, [expression])!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new EntityQueryable<TElement>(this,expression);
        }

        public object Execute(Expression expression)
        {
            return GenericExecuteMethod.MakeGenericMethod(expression.Type)
            .Invoke(_queryCompiler, [expression])!;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _queryCompiler.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            return _queryCompiler.ExecuteAsync<TResult>(expression, cancellationToken);
        }

        public IDbBus<TElement> CreateBus<TElement>(Expression expression)
        {
            return new EntityQueryable<TElement>(this, expression);
        }
    }
}
