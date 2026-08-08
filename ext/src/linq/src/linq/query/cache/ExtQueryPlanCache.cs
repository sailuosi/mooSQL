using mooSQL.data;
using mooSQL.data.clip;
using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq
{
	/// <summary>
	/// Global query plan cache for Ext LINQ compilation results.
	/// </summary>
	internal sealed class ExtQueryPlanCache
	{
		public static ExtQueryPlanCache Instance { get; }

		readonly TimeSpan _defaultExpiration;
		FrequencyBasedCache<ExtQueryCacheKey, SentenceBag> _cache;

		static ExtQueryPlanCache()
		{
			Instance = new ExtQueryPlanCache();
			QueryRunner.CacheCleaners.Enqueue(() => Instance.Clear());
		}

		ExtQueryPlanCache()
		{
			_defaultExpiration = TimeSpan.FromMinutes(10);
			_cache = CreateCache(_defaultExpiration);
		}

		static FrequencyBasedCache<ExtQueryCacheKey, SentenceBag> CreateCache(TimeSpan expiration)
			=> new(expiration);

		static ExtQueryCacheKey BuildKey(DBInstance db, Expression expr, Type resultType, QueryFlags flags)
		{
			var opt = db.dialect.Option;
			return new ExtQueryCacheKey(
				expr,
				resultType,
				db.dialect.GetType(),
				flags,
				opt.ParameterizeTakeSkip,
				opt.PreferApply);
		}

		public SentenceBag? Find(DBInstance db, Expression expr, Type resultType, QueryFlags flags)
		{
			var key = BuildKey(db, expr, resultType, flags);

			if (_cache.TryGetValue(key, out var query))
			{
				query.DBLive = db;
				return query;
			}

			return null;
		}

		public void TryAdd(DBInstance db, Expression expr, Type resultType, QueryFlags flags, SentenceBag query)
		{
			if (!query.IsCacheable || query.ErrorExpression != null)
				return;

			var key = BuildKey(db, expr, resultType, flags);
			_cache.Add(key, query);
		}

		public void Clear()
		{
			_cache = CreateCache(_defaultExpiration);
		}
	}
}
