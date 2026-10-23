using System;
using System.Collections.Concurrent;
using mooSQL.linq.utils;

namespace mooSQL.linq
{
	/// <summary>
	/// QueryRunner.Cache type.
	/// </summary>
	/// <summary>
	/// QueryRunner.Cache type.
	/// </summary>
	public static partial class QueryRunner
	{
		internal static readonly ConcurrentQueue<Action> CacheCleaners = new();

		static QueryRunner()
		{
			CacheCleaners.Enqueue(Compilation.ClearLambdaCache);
		}

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
