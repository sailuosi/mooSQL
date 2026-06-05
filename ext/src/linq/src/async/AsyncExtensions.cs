using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace mooSQL.linq.ext
{
	using Linq;

	/// <summary>
	/// Provides helper methods for asynchronous operations.
	/// </summary>
	
	public static partial class AsyncExtensions
	{
		#region Helpers
		/// <summary>
		/// Executes provided action using task scheduler.
		/// </summary>
		/// <param name="action">Action to execute.</param>
		/// <param name="token">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		internal static Task GetActionTask(Action action, CancellationToken token)
		{
			var task = new Task(action, token);

			task.Start();

			return task;
		}

		/// <summary>
		/// Executes provided function using task scheduler.
		/// </summary>
		/// <typeparam name="T">Function result type.</typeparam>
		/// <param name="func">Function to execute.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		internal static Task<T> GetTask<T>(Func<T> func)
		{
			var task = new Task<T>(func);

			task.Start();

			return task;
		}

		/// <summary>
		/// Executes provided function using task scheduler.
		/// </summary>
		/// <typeparam name="T">Function result type.</typeparam>
		/// <param name="func">Function to execute.</param>
		/// <param name="token">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		static Task<T> GetTask<T>(Func<T> func, CancellationToken token)
		{
			var task = new Task<T>(func, token);

			task.Start();

			return task;
		}

		#endregion

		[AttributeUsage(AttributeTargets.Method)]
		internal sealed class ElementAsyncAttribute : Attribute
		{
		}



        #region ForEachAsync



		/// <summary>
		/// Asynchronously apply provided function to each element in source sequence sequentially.
		/// Sequence enumeration stops if function returns <c>false</c>.
		/// </summary>
		/// <typeparam name="TSource">Source sequence element type.</typeparam>
		/// <param name="source">Source sequence.</param>
		/// <param name="func">Function to apply to each sequence element. Returning <c>false</c> from function will stop numeration.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public static Task ForEachUntilAsync<TSource>(
			this IQueryable<TSource> source, Func<TSource,bool> func,
			CancellationToken token = default)
		{
			if (source is ExpressionQuery<TSource> query)
				return query.GetForEachUntilAsync(func, token);

			return GetActionTask(() =>
			{
				foreach (var item in source)
					if (token.IsCancellationRequested || !func(item))
						break;
			},
			token);
		}

		#endregion



	}
}
