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

        /// <summary>
        /// 当前查询编译/执行所绑定的数据库实例。
        /// </summary>
        protected DBInstance DB;

        /// <summary>
        /// 使用指定的数据库实例创建查询编译器。
        /// </summary>
        /// <param name="DB">数据库实例。</param>
        public BaseQueryCompiler(DBInstance DB) { 
            this.DB = DB;
        }

        private QueryContext GetContext() {
            var context = new QueryContext();
            context.DB = DB;
            return context;
        }

        /// <summary>
        /// 将 LINQ 表达式编译为可在给定 <see cref="QueryContext"/> 上执行的委托（同步路径）。
        /// </summary>
        /// <typeparam name="TResult">查询结果类型。</typeparam>
        /// <param name="query">查询表达式树。</param>
        /// <returns>已编译的执行委托。</returns>
        public Func<QueryContext, TResult> CreateCompiledQuery<TResult>(Expression query)
        {
            var context = GetContext();
            var queryNext = PrepareExpression(query);

            var fun = DoCompile<TResult>(queryNext, context);
            return fun;
        }

        /// <summary>
        /// 将 LINQ 表达式编译为可在给定 <see cref="QueryContext"/> 上执行的委托（异步查询使用的编译入口）。
        /// </summary>
        /// <typeparam name="TResult">查询结果类型。</typeparam>
        /// <param name="query">查询表达式树。</param>
        /// <returns>已编译的执行委托。</returns>
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

        /// <summary>
        /// 在给定查询上下文中执行实际的表达式编译，生成可调用委托。
        /// </summary>
        /// <typeparam name="TResult">查询结果类型。</typeparam>
        /// <param name="expression">已预处理后的表达式。</param>
        /// <param name="context">查询上下文。</param>
        /// <returns>编译得到的执行委托。</returns>
        public abstract Func<QueryContext, TResult> DoCompile<TResult>(Expression expression,QueryContext context);
    }
}
