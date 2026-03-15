using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq.Builder
{
	using Async;
	using Common;
	using Common.Internal;
	using Extensions;
	using Reflection;
	using SqlQuery;
	using mooSQL.linq.Expressions;

	class Preamble<TKey, T> : Preamble
		where TKey : notnull
	{
		readonly SentenceBag<KeyDetailEnvelope<TKey, T>> _query;

		public Preamble(SentenceBag<KeyDetailEnvelope<TKey, T>> query)
		{
			_query = query;
		}

		public override object Execute(RunnerContext context)
		{
			var result = new PreambleResult<TKey, T>();
			foreach (var e in _query.Runner.loadResultList(context))
			{
				result.Add(e.Key, e.Detail);
			}

			return result;
		}

		public override async Task<object> ExecuteAsync(RunnerContext context)
		{
			var result = new PreambleResult<TKey, T>();
#if NET462 || NETSTANDARD2_0 || NET451
            var enumerator = _query.Runner.loadResultList(context)
                    .GetEnumerator();

            while (enumerator.MoveNext())
            {
                var e = enumerator.Current;
                result.Add(e.Key, e.Detail);
            }

            return result;
#else
            var enumerator = _query.Runner.loadResultList(context)
					.GetAsyncEnumerator(context.cancellationToken);

			while (await enumerator.MoveNextAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext))
			{
				var e = enumerator.Current;
				result.Add(e.Key, e.Detail);
			}

			return result;
#endif

        }
	}
	class PreambleResult<TKey, T>
	where TKey : notnull
	{
		Dictionary<TKey, List<T>>? _items;
		TKey                       _prevKey = default!;
		List<T>?                   _prevList;

		public void Add(TKey key, T item)
		{
			List<T>? list;

			if (_prevList != null && _prevKey!.Equals(key))
			{
				list = _prevList;
			}
			else
			{
				if (_items == null)
				{
					_items = new Dictionary<TKey, List<T>>();
					list = new List<T>();
					_items.Add(key, list);
				}
				else if (!_items.TryGetValue(key, out list))
				{
					list = new List<T>();
					_items.Add(key, list);
				}

				_prevKey = key;
				_prevList = list;
			}

			list.Add(item);
		}

		public List<T> GetList(TKey key)
		{
			if (_items == null || !_items.TryGetValue(key, out var list))
				return new List<T>();
			return list;
		}
	}
	struct KeyDetailEnvelope<TKey, TDetail>
	where TKey : notnull
	{
#pragma warning disable CS0649 // Field is never assigned: used by expressions
		public TKey    Key;
		public TDetail Detail;
#pragma warning restore CS0649 // Field is never assigned
	}
}
