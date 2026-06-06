#if NET5_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq.Async
{
	internal static class AsyncEnumerableExtensions
	{
		public static async Task<List<T>> ToListAsync<T>(
			this IAsyncEnumerable<T> source,
			CancellationToken        cancellationToken = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var result = new List<T>();
			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
			{
				while (await enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
				{
					result.Add(enumerator.Current);
				}
			}

			return result;
		}

		public static async Task<T[]> ToArrayAsync<T>(
			this IAsyncEnumerable<T> source,
			CancellationToken        cancellationToken = default)
		{
			return (await source.ToListAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext)).ToArray();
		}

		public static async Task<T?> FirstOrDefaultAsync<T>(
			this IAsyncEnumerable<T> source,
			CancellationToken        cancellationToken = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
			{
				if (await enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					return enumerator.Current;
				return default;
			}
		}

		public static async Task<TSource> FirstAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var enumerator = source.GetAsyncEnumerator(token);
			await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
			{
				if (await enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					return enumerator.Current;
			}

			throw new InvalidOperationException("The source sequence is empty.");
		}

		public static async Task<TSource?> SingleOrDefaultAsync<TSource>(
			this IAsyncEnumerable<TSource> source,
			CancellationToken              cancellationToken = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
			{
				if (!await enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					return default;

				var first = enumerator.Current;
				if (await enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					throw new InvalidOperationException("The input sequence contains more than one element.");

				return first;
			}
		}

		public static async Task<TSource> SingleAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
			{
				if (!await enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					throw new InvalidOperationException("Sequence contains no elements.");

				var first = enumerator.Current;
				if (await enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					throw new InvalidOperationException("Sequence contains more than one element.");

				return first;
			}
		}
	}
}
#endif
