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
        /// Deletes records from source query and returns deleted records.
        /// </summary>
        /// <typeparam name="TSource">Source query record type.</typeparam>
        /// <param name="source">Source query, that returns data for delete operation.</param>
        /// <returns>Enumeration of records.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// <item>PostgreSQL</item>
        /// <item>SQLite 3.35+</item>
        /// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
        /// </list>
        /// </remarks>
        public static IEnumerable<TSource> DeleteWithOutput<TSource>(this IQueryable<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));


            return source.Provider.CreateQuery<TSource>(
                    Expression.Call(
                        null,
                        MethodHelper.GetMethodInfo(DeleteWithOutput, source),
                        source.Expression))
                .AsEnumerable();
        }

        /// <summary>
        /// Deletes records from source query into target table and returns deleted records.
        /// </summary>
        /// <typeparam name="TSource">Source query record type.</typeparam>
        /// <typeparam name="TOutput">Output table record type.</typeparam>
        /// <param name="source">Source query, that returns data for delete operation.</param>
        /// <param name="outputExpression">Output record constructor expression.
        /// Expression supports only record new expression with field initializers.</param>
        /// <returns>Enumeration of records.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// <item>PostgreSQL</item>
        /// <item>SQLite 3.35+</item>
        /// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
        /// </list>
        /// </remarks>

        public static IEnumerable<TOutput> DeleteWithOutput<TSource, TOutput>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, TOutput>> outputExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));


            return source.Provider.CreateQuery<TOutput>(
                    Expression.Call(
                        null,
                        MethodHelper.GetMethodInfo(DeleteWithOutput, source, outputExpression),
                        source.Expression,
                        Expression.Quote(outputExpression)))
                .AsEnumerable();
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Deletes records from source query into target table asynchronously and returns deleted records.
        /// </summary>
        /// <typeparam name="TSource">Source query record type.</typeparam>
        /// <typeparam name="TOutput">Output table record type.</typeparam>
        /// <param name="source">Source query, that returns data for delete operation.</param>
        /// <param name="outputExpression">Output record constructor expression.
        /// Expression supports only record new expression with field initializers.</param>
        /// <returns>Async sequence of records returned by output.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// <item>PostgreSQL</item>
        /// <item>SQLite 3.35+</item>
        /// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
        /// </list>
        /// </remarks>
        public static IAsyncEnumerable<TOutput> DeleteWithOutputAsync<TSource, TOutput>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, TOutput>> outputExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));


            return source.Provider.CreateQuery<TOutput>(
                    Expression.Call(
                        null,
                        MethodHelper.GetMethodInfo(DeleteWithOutput, source, outputExpression),
                        source.Expression,
                        Expression.Quote(outputExpression)))
                .AsAsyncEnumerable();
        }

        /// <summary>
        /// Deletes records from source query into target table asynchronously and returns deleted records.
        /// </summary>
        /// <typeparam name="TSource">Source query record type.</typeparam>
        /// <param name="source">Source query, that returns data for delete operation.</param>
        /// <returns>Async sequence of records returned by output.</returns>
        /// <remarks>
        /// Database support:
        /// <list type="bullet">
        /// <item>SQL Server 2005+</item>
        /// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
        /// <item>PostgreSQL</item>
        /// <item>SQLite 3.35+</item>
        /// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
        /// </list>
        /// </remarks>
        public static IAsyncEnumerable<TSource> DeleteWithOutputAsync<TSource>(
            this IQueryable<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));


            return source.Provider.CreateQuery<TSource>(
                    Expression.Call(
                        null,
                        MethodHelper.GetMethodInfo(DeleteWithOutput, source),
                        source.Expression))
                .AsAsyncEnumerable();
        }
#else

#endif
    }
}
