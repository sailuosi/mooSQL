using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq
{
	using Common;
	using Common.Internal.Cache;

    using mooSQL.data;
	using Tools;


	static partial class QueryRunner
	{

        internal static readonly ConcurrentQueue<Action> CacheCleaners = new();

        /// <summary>
        /// Clears query caches for all typed queries.
        /// </summary>
        public static void ClearCaches()
        {
            foreach (var cleaner in CacheCleaners)
            {
                cleaner();
            }
        }
        public static class Cache<T>
		{
			static Cache()
			{
				CacheCleaners.Enqueue(ClearCache);
			}

			public static void ClearCache()
			{
				QueryCache.Clear();
			}

			internal static MemoryCache<IStructuralEquatable,SentenceBag<T>> QueryCache { get; } = new(new());
		}

		public static class Cache<T,TR>
		{
			static Cache()
			{
				CacheCleaners.Enqueue(ClearCache);
			}

			public static void ClearCache()
			{
				QueryCache.Clear();
			}

			internal static MemoryCache<IStructuralEquatable,SentenceBag<TR>> QueryCache { get; } = new(new());
		}
	}
}
