using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.data;

namespace mooSQL.data.richRepo
{
    /// <summary>
    /// 实体字典缓存（按类型 + 库名）。与查询结果缓存分离。
    /// </summary>
    internal sealed class EntityCacheStore<T> where T : class, new()
    {
        static readonly ConcurrentDictionary<string, CacheEntry> Global =
            new ConcurrentDictionary<string, CacheEntry>(StringComparer.Ordinal);

        readonly string _key;
        readonly int _ttlSeconds;

        public EntityCacheStore(string databaseName, Type entityType, int ttlSeconds)
        {
            _key = (entityType?.FullName ?? typeof(T).FullName) + "|" + (databaseName ?? "");
            _ttlSeconds = ttlSeconds > 0 ? ttlSeconds : 300;
        }

        public Dictionary<string, T> GetOrWarm(Func<Dictionary<string, T>> warm)
        {
            if (Global.TryGetValue(_key, out var entry) && !entry.IsExpired(_ttlSeconds))
                return entry.Map;

            var map = warm() ?? new Dictionary<string, T>(StringComparer.Ordinal);
            Global[_key] = new CacheEntry { Map = map, CreatedUtc = DateTime.UtcNow };
            return map;
        }

        public void Clear()
        {
            Global.TryRemove(_key, out _);
        }

        sealed class CacheEntry
        {
            public Dictionary<string, T> Map;
            public DateTime CreatedUtc;
            public bool IsExpired(int ttl) => (DateTime.UtcNow - CreatedUtc).TotalSeconds > ttl;
        }
    }
}
