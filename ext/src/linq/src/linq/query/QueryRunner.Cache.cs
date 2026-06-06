using System;
using System.Collections.Concurrent;

namespace mooSQL.linq.Linq
{
	static partial class QueryRunner
	{
		internal static readonly ConcurrentQueue<Action> CacheCleaners = new();

		/// <summary>
		/// Clears registered LINQ compile caches.
		/// </summary>
		public static void ClearCaches()
		{
			foreach (var cleaner in CacheCleaners)
			{
				cleaner();
			}
		}
	}
}
