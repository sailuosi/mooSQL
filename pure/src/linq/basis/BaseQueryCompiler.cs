using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 编译器
    /// </summary>
    public abstract class BaseQueryCompiler : IQueryCompiler
    {

        protected DBInstance DB;

        public BaseQueryCompiler(DBInstance DB) { 
            this.DB = DB;
        }

        private QueryContext GetContext() {
            var context = new QueryContext();
            context.DB = DB;
            return context;
        }

        public Func<QueryContext, TResult> CreateCompiledQuery<TResult>(Expression query)
        {
            var context = GetContext();
            var queryNext = PrepareExpression(query);

            var fun = DoCompile<TResult>(queryNext, context);
            return fun;
        }

        public Func<QueryContext, TResult> CreateCompiledAsyncQuery<TResult>(Expression query)
        {
            var context = GetContext();
            var queryNext = PrepareExpression(query);

            var fun = DoCompile<TResult>(queryNext, context);
            return fun;
        }


        /// <summary>
        /// 执行
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public TResult Execute<TResult>(Expression query)
        {
            var context = GetContext();
            
            var queryNext = PrepareExpression(query);

            var fun = DoCompile<TResult>(queryNext,context);
            return fun(context);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public TResult ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken)
        {
            var context = GetContext();
            context.cancellationToken = cancellationToken;
            var queryNext = PrepareExpression(query);

            var fun = DoCompile<TResult>(queryNext,context);
            return fun(context);
        }

        /// <summary>
        /// 对表达式进行前置的处理
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public virtual Expression PrepareExpression(Expression expression) {
            return expression;
        }

        public abstract Func<QueryContext, TResult> DoCompile<TResult>(Expression expression,QueryContext context);
    }
}
