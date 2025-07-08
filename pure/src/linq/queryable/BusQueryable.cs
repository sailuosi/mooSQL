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
    /// 增强的update/delete语句方法
    /// </summary>
    public static partial class BusQueryable
    {
        /// <summary>
        /// 增加Queryable有时需要升格，来调用个性化的方法的问题；
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public static IDbBus<T> ToBus<T>(this IQueryable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is IDbBus<T> bus) { 
                return bus;
            }
            throw new NotImplementedException("当前实例"+nameof(source)+"不是DbBus的实现");
        }
        /// <summary>
        /// 注入SQL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="injectedSQL"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IQueryable<T> InjectSQL<T>(this IQueryable<T> source,Action<SQLBuilder,FastCompileContext> injectedSQL) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (injectedSQL == null) throw new ArgumentNullException(nameof(injectedSQL));
            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Action<SQLBuilder, FastCompileContext>, IQueryable<T>>(InjectSQL).Method,
                    source.Expression,Expression.Constant(injectedSQL)));
        }

        /// <summary>
        /// 批量的设置，set赋值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="setter"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IQueryable<T> Set<T>(this IQueryable<T> source, Expression<Func<T, T>> setter)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (setter == null) throw new ArgumentNullException(nameof(setter));

            //

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, T>>, IQueryable<T>>(Set).Method,
                    source.Expression, Expression.Quote(setter)));
        }
        /// <summary>
        /// 单个字段的set赋值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TV"></typeparam>
        /// <param name="source"></param>
        /// <param name="setter"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IQueryable<T> Set<T, TV>(this IQueryable<T> source, Expression<Func<T, TV>> setter,TV value)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (setter == null) throw new ArgumentNullException(nameof(setter));

            //

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, TV>>, TV, IQueryable<T>>(Set).Method,
                    source.Expression, setter,Expression.Constant(value)));
        }
        /// <summary>
        /// 执行update
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static int DoUpdate<T>(this IQueryable<T> source) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>,int>(DoUpdate).Method,
                    source.Expression
                    )    
            );
        }
        /// <summary>
        /// 执行删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static int DoDelete<T>(this IQueryable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source.Provider.Execute<int>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, int>(DoDelete).Method,
                    source.Expression
                    )
            );
        }
        /// <summary>
        /// 原始的SQLBuilder sink暴露
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IQueryable<T> Sink<T>(this IQueryable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IQueryable<T>>(Sink).Method,
                    source.Expression));
        }
        public static IQueryable<T> SinkOR<T>(this IQueryable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IQueryable<T>>(SinkOR).Method,
                    source.Expression));
        }
        public static IQueryable<T> Rise<T>(this IQueryable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IQueryable<T>>(Rise).Method,
                    source.Expression));
        }

        /// <summary>
        /// 翻页方法  setPage(int size, int num)的支持
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNum"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IQueryable<T> SetPage<T>(this IQueryable<T> source,int pageSize,int pageNum)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>,int,int, IQueryable<T>>(SetPage).Method,
                    source.Expression,Expression.Constant(pageSize,typeof(int)), Expression.Constant(pageNum, typeof(int))

                    ));
        }

        public static IQueryable<T> Top<T>(this IQueryable<T> source, int pageSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, int, IQueryable<T>>(Top).Method,
                    source.Expression, Expression.Constant(pageSize, typeof(int))
                    ));
        }

        public static PageOutput<T> ToPageList<T>(this IQueryable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return source.Provider.Execute<PageOutput<T>>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, PageOutput<T>>(ToPageList).Method,
                    source.Expression
                    ));
        }
        public static PageOutput<T> ToPageList<T>(this IQueryable<T> source, int pageSize,int pageNum)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return source.Provider.Execute<PageOutput<T>>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, int,int, PageOutput<T>>(ToPageList).Method,
                    source.Expression, 
                    Expression.Constant(pageSize, typeof(int)),
                    Expression.Constant(pageNum, typeof(int))
                    ));
        }

        /// <summary>
        /// 超自由的无参join写法，类的声明由捕获外界变量处理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="joinString"></param>
        /// <param name="onCondition"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IQueryable<T> Join<T>(this IQueryable<T> src,string joinString, Expression<Func<bool>> onCondition)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (onCondition == null) throw new ArgumentNullException(nameof(onCondition));

            return src.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>,string, Expression<Func< bool>>, IQueryable<T>>(Join).Method,
                    src.Expression, Expression.Constant(joinString),Expression.Quote(onCondition)
                    ));
        }

        /// <summary>
        /// .LeftJoin((a,b)=>a.id==b.id)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <param name="src"></param>
        /// <param name="onCondition"></param>
        /// <returns></returns>
        public static IQueryable<T> LeftJoin<E,T>(this IQueryable<T> src, Expression<Func<T, E, bool>> onCondition) {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (onCondition == null) throw new ArgumentNullException(nameof(onCondition));

            return src.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, E, bool>>, IQueryable<T>>(LeftJoin).Method,
                    src.Expression,Expression.Quote(onCondition)
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
        public static IQueryable<T> InnerJoin<E,T>(this IQueryable<T> src, Expression<Func<T, E, bool>> onCondition)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (onCondition == null) throw new ArgumentNullException(nameof(onCondition));

            return src.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, E, bool>>, IQueryable<T>>(InnerJoin).Method,
                    src.Expression, Expression.Quote(onCondition)
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
        public static IQueryable<T> RightJoin<E,T>(this IQueryable<T> src, Expression<Func<T, E, bool>> onCondition)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (onCondition == null) throw new ArgumentNullException(nameof(onCondition));

            return src.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, E, bool>>, IQueryable<T>>(RightJoin).Method,
                    src.Expression, Expression.Quote(onCondition)
                    ));
        }

        ///// <summary>
        ///// 查询
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <typeparam name="TV"></typeparam>
        ///// <param name="source"></param>
        ///// <param name="setter"></param>
        ///// <returns></returns>
        ///// <exception cref="ArgumentNullException"></exception>
        //public static IQueryable<TV> Select<T, TV>(this IQueryable<T> source, Expression<Func<T, TV>> setter)
        //{
        //    if (source == null) throw new ArgumentNullException(nameof(source));
        //    if (setter == null) throw new ArgumentNullException(nameof(setter));

        //    //

        //    return source.Provider.CreateQuery<TV>(
        //        Expression.Call(
        //            null,
        //            new Func<IQueryable<T>, Expression<Func<T, TV>>, IQueryable<TV>>(Select).Method,
        //            source.Expression, setter));
        //}

        public static IQueryable<T> Includes<T, R>(this IQueryable<T> src, Expression<Func<T, List<R>>> selector) {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return src.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, List<R>>>, IQueryable<T>>(Includes).Method,
                    src.Expression, selector
                    ));
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> src, Expression<Func<bool>> Condition)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (Condition == null) throw new ArgumentNullException(nameof(Condition));

            return src.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<bool>>, IQueryable<T>>(Where).Method,
                    src.Expression, Expression.Quote(Condition)
                    ));
        }
    }


}
