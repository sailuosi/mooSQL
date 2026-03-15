using mooSQL.linq.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;


namespace mooSQL.linq.ext
{


	public static partial class LinqExtensions
	{












		/// <summary>
		/// Deletes records from source query into target table and outputs deleted records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int DeleteWithOutputInto<TSource,TOutput>(
			this IQueryable<TSource> source,
			ITable<TOutput>          outputTable)
			where TOutput : notnull
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable),
					currentSource.Expression,
					((IQueryable<TOutput>)outputTable).Expression));
		}

		/// <summary>
		/// Deletes records from source query into target table asynchronously and outputs deleted records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> DeleteWithOutputIntoAsync<TSource,TOutput>(
			this IQueryable<TSource> source,
			ITable<TOutput>          outputTable,
			CancellationToken        token = default)
			where TOutput : notnull
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable),
					currentSource.Expression,
					((IQueryable<TOutput>)outputTable).Expression);

			if (source is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<int>(expr, token);

			return Task.Run(() => source.Provider.Execute<int>(expr), token);
		}

		/// <summary>
		/// Deletes records from source query into target table and outputs deleted records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int DeleteWithOutputInto<TSource,TOutput>(
			this IQueryable<TSource>          source,
			ITable<TOutput>                   outputTable,
			Expression<Func<TSource,TOutput>> outputExpression)
			where TOutput : notnull
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable, outputExpression),
					currentSource.Expression,
					((IQueryable<TOutput>)outputTable).Expression,
					Expression.Quote(outputExpression)));
		}

		/// <summary>
		/// Deletes records from source query into target table asynchronously and outputs deleted records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> DeleteWithOutputIntoAsync<TSource,TOutput>(
			this IQueryable<TSource>          source,
			ITable<TOutput>                   outputTable,
			Expression<Func<TSource,TOutput>> outputExpression,
			CancellationToken                 token = default)
			where TOutput : notnull
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable, outputExpression),
					currentSource.Expression,
					((IQueryable<TOutput>)outputTable).Expression,
					Expression.Quote(outputExpression));

			if (currentSource is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<int>(expr, token);

			return Task.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}


    }
}
