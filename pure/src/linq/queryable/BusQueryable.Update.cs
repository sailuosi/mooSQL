using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public static partial class BusQueryable
    {


        /// <summary>
        /// Executes update-from-source operation against target table.
        /// </summary>
        /// <typeparam name="TSource">Source query record type.</typeparam>
        /// <typeparam name="TTarget">Target table mapping class.</typeparam>
        /// <typeparam name="TOutput">Output table record type.</typeparam>
        /// <param name="source">Source data query.</param>
        /// <param name="target">Target table.</param>
        /// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
        /// <param name="outputExpression">Output record constructor expression.
        /// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
        /// Expression supports only record new expression with field initializers.</param>
        /// <returns>Output values from the update statement.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// <item>PostgreSQL (doesn't support old data; database limitation)</item>
        /// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
        /// </list>
        /// </remarks>
        public static IEnumerable<TOutput> UpdateWithOutput<TSource, TTarget, TOutput>(
                            this IQueryable<TSource> source,
                            Expression<Func<TSource, TTarget>> target,
                            Expression<Func<TSource, TTarget>> setter,
                            Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (setter == null) throw new ArgumentNullException(nameof(setter));
            if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));


            return source.Provider.CreateQuery<TOutput>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, 
                        Expression<Func<TSource, TTarget>>, 
                        Expression<Func<TSource, TTarget>>,
                        Expression<Func<TSource, TTarget, TTarget, TOutput>>,
                        IEnumerable<TOutput>
                        >(UpdateWithOutput).Method,
                    source.Expression,
                    Expression.Quote(target),
                    Expression.Quote(setter),
                    Expression.Quote(outputExpression)));
        }

        /// <summary>
        /// Executes update-from-source operation against target table.
        /// </summary>
        /// <typeparam name="TSource">Source query record type.</typeparam>
        /// <typeparam name="TTarget">Target table mapping class.</typeparam>
        /// <param name="source">Source data query.</param>
        /// <param name="target">Target table.</param>
        /// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
        /// <returns>Deleted and inserted values for every record updated.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// </list>
        /// </remarks>
        public static IEnumerable<UpdateOutput<TTarget>> UpdateWithOutput<TSource, TTarget>(
                            this IQueryable<TSource> source,
                            Expression<Func<TSource, TTarget>> target,
                            Expression<Func<TSource, TTarget>> setter)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (setter == null) throw new ArgumentNullException(nameof(setter));


            return source.Provider.CreateQuery<UpdateOutput<TTarget>>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, 
                        Expression<Func<TSource, TTarget>>, 
                        Expression<Func<TSource, TTarget>>, 
                        IEnumerable<UpdateOutput<TTarget>>>
                    (UpdateWithOutput).Method,
                    source.Expression,
                    Expression.Quote(target),
                    Expression.Quote(setter)));
        }

        /// <summary>
        /// Executes update operation using source query as record filter.
        /// </summary>
        /// <typeparam name="T">Updated table record type.</typeparam>
        /// <param name="source">Source data query.</param>
        /// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
        /// <returns>Deleted and inserted values for every record updated.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// </list>
        /// </remarks>
        public static IEnumerable<UpdateOutput<T>> UpdateWithOutput<T>(
                       this IQueryable<T> source,
                                     Expression<Func<T, T>> setter)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (setter == null) throw new ArgumentNullException(nameof(setter));


            return source.Provider.CreateQuery<UpdateOutput<T>>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, T>>, IEnumerable<UpdateOutput<T>>>(UpdateWithOutput).Method,
                    source.Expression, Expression.Quote(setter)));
        }

        /// <summary>
        /// Executes update operation using source query as record filter.
        /// </summary>
        /// <typeparam name="T">Updated table record type.</typeparam>
        /// <typeparam name="TOutput">Output table record type.</typeparam>
        /// <param name="source">Source data query.</param>
        /// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
        /// <param name="outputExpression">Output record constructor expression.
        /// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
        /// Expression supports only record new expression with field initializers.</param>
        /// <returns>Output values from the update statement.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// <item>PostgreSQL (doesn't support old data; database limitation)</item>
        /// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
        /// </list>
        /// </remarks>
        public static IEnumerable<TOutput> UpdateWithOutput<T, TOutput>(
                       this IQueryable<T> source,
                                     Expression<Func<T, T>> setter,
                            Expression<Func<T, T, TOutput>> outputExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (setter == null) throw new ArgumentNullException(nameof(setter));
            if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));



            return source.Provider.CreateQuery<TOutput>(
                Expression.Call(
                    null,
                    MethodHelper.GetMethodInfo(UpdateWithOutput, source, setter, outputExpression),
                    source.Expression, Expression.Quote(setter),
                    Expression.Quote(outputExpression)));
        }


#if NET6_0_OR_GREATER
        /// <summary>
        /// Executes update-from-source operation against target table.
        /// </summary>
        /// <typeparam name="TSource">Source query record type.</typeparam>
        /// <typeparam name="TTarget">Target table mapping class.</typeparam>
        /// <typeparam name="TOutput">Output table record type.</typeparam>
        /// <param name="source">Source data query.</param>
        /// <param name="target">Target table.</param>
        /// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
        /// <param name="outputExpression">Output record constructor expression.
        /// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
        /// Expression supports only record new expression with field initializers.</param>
        /// <returns>Async sequence of records returned by output.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// <item>PostgreSQL (doesn't support old data; database limitation)</item>
        /// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
        /// </list>
        /// </remarks>
        public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<TSource, TTarget, TOutput>(
							this IQueryable<TSource> source,
							Expression<Func<TSource, TTarget>> target,
			                         Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));



			return source.Provider.CreateQuery<TOutput>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter, outputExpression),
                    source.Expression,
					Expression.Quote(target),
					Expression.Quote(setter),
					Expression.Quote(outputExpression)))
				.AsAsyncEnumerable();
		}

        /// <summary>
        /// Executes update operation using source query as record filter.
        /// </summary>
        /// <typeparam name="T">Updated table record type.</typeparam>
        /// <param name="source">Source data query.</param>
        /// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
        /// <returns>Deleted and inserted values for every record updated.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// </list>
        /// </remarks>
        public static IAsyncEnumerable<UpdateOutput<T>> UpdateWithOutputAsync<T>(
                       this IQueryable<T> source,
                                     Expression<Func<T, T>> setter)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (setter == null) throw new ArgumentNullException(nameof(setter));



            return source.Provider.CreateQuery<UpdateOutput<T>>(
                Expression.Call(
                    null,
                    MethodHelper.GetMethodInfo(UpdateWithOutput, source, setter),
                    source.Expression, Expression.Quote(setter)))
                .AsAsyncEnumerable();
        }


        /// <summary>
        /// Executes update-from-source operation against target table.
        /// </summary>
        /// <typeparam name="TSource">Source query record type.</typeparam>
        /// <typeparam name="TTarget">Target table mapping class.</typeparam>
        /// <param name="source">Source data query.</param>
        /// <param name="target">Target table.</param>
        /// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
        /// <returns>Deleted and inserted values for every record updated.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// </list>
        /// </remarks>
        public static IAsyncEnumerable<UpdateOutput<TTarget>> UpdateWithOutputAsync<TSource, TTarget>(
                            this IQueryable<TSource> source,
                            Expression<Func<TSource, TTarget>> target,
                                     Expression<Func<TSource, TTarget>> setter)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (setter == null) throw new ArgumentNullException(nameof(setter));

            return source.Provider.CreateQuery<UpdateOutput<TTarget>>(
                Expression.Call(
                    null,
                    MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter),
                    source.Expression,
                    Expression.Quote(target),
                    Expression.Quote(setter)))
                .AsAsyncEnumerable();
        }

        /// <summary>
        /// Executes update operation using source query as record filter.
        /// </summary>
        /// <typeparam name="T">Updated table record type.</typeparam>
        /// <typeparam name="TOutput">Output table record type.</typeparam>
        /// <param name="source">Source data query.</param>
        /// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
        /// <param name="outputExpression">Output record constructor expression.
        /// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
        /// Expression supports only record new expression with field initializers.</param>
        /// <returns>Async sequence of records returned by output.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// <item>PostgreSQL (doesn't support old data; database limitation)</item>
        /// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
        /// </list>
        /// </remarks>
        public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<T, TOutput>(
                       this IQueryable<T> source,
                                     Expression<Func<T, T>> setter,
                            Expression<Func<T, T, TOutput>> outputExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (setter == null) throw new ArgumentNullException(nameof(setter));
            if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));


            return source.Provider.CreateQuery<TOutput>(
                Expression.Call(
                    null,
                    MethodHelper.GetMethodInfo(UpdateWithOutput, source, setter, outputExpression),
                    source.Expression, Expression.Quote(setter),
                    Expression.Quote(outputExpression)))
                .AsAsyncEnumerable();
        }
#endif


#if NET5_0_OR_GREATER

        /// <summary>
        /// Returns an <see cref="IAsyncEnumerable{T}"/> that can be enumerated asynchronously.
        /// </summary>
        /// <typeparam name="TSource">Source sequence element type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <returns>A query that can be enumerated asynchronously.</returns>
        public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(this IQueryable<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (source is IAsyncEnumerable<TSource> asyncQuery)
                return asyncQuery;

            // return an enumerator that will synchronously enumerate the source elements
            return new AsyncEnumerableAdapter<TSource>(source);
        }

        private sealed class AsyncEnumerableAdapter<T> : IAsyncEnumerable<T>
        {
            private readonly IQueryable<T> _query;
            public AsyncEnumerableAdapter(IQueryable<T> query)
            {
                _query = query ?? throw new ArgumentNullException(nameof(query));
            }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                using var enumerator = _query.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }


        }
#endif
    }
}
