using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace mooSQL.data
{
    /// <summary>
    /// 基于 <see cref="MemoryCache"/>（System.Runtime.Caching）的 <see cref="ISooCache"/> 实现。
    /// 无显式 TTL 时使用 <see cref="DefaultExpiration"/>（默认 12 小时）。
    /// 示例：<c>client.useCache(new MemorySooCache(TimeSpan.FromHours(6)));</c>
    /// </summary>
    public class MemorySooCache : ISooCache, IDisposable
    {
        private readonly MemoryCache _cache;
        private readonly bool _ownsCache;
        private readonly ConcurrentDictionary<string, byte> _keys = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, object> _createLocks = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);

        /// <summary>使用独立 <see cref="MemoryCache"/> 实例，默认过期 12 小时。</summary>
        public MemorySooCache()
            : this(SooCacheDefaults.Expiration)
        {
        }

        /// <summary>使用独立 <see cref="MemoryCache"/> 实例，并指定无显式 TTL 时的默认过期。</summary>
        public MemorySooCache(TimeSpan defaultExpiration)
            : this(new MemoryCache(nameof(MemorySooCache) + Guid.NewGuid().ToString("N")), ownsCache: true, defaultExpiration)
        {
        }

        /// <summary>使用独立 <see cref="MemoryCache"/> 实例，并指定无显式 TTL 时的默认过期秒数。</summary>
        public MemorySooCache(int defaultExpirationSeconds)
            : this(TimeSpan.FromSeconds(defaultExpirationSeconds))
        {
        }

        /// <summary>注入外部 <see cref="MemoryCache"/>（不负责 Dispose），默认过期 12 小时。</summary>
        public MemorySooCache(MemoryCache cache)
            : this(cache, ownsCache: false, SooCacheDefaults.Expiration)
        {
        }

        /// <summary>注入外部 <see cref="MemoryCache"/>，并指定默认过期。</summary>
        public MemorySooCache(MemoryCache cache, TimeSpan defaultExpiration)
            : this(cache, ownsCache: false, defaultExpiration)
        {
        }

        private MemorySooCache(MemoryCache cache, bool ownsCache, TimeSpan defaultExpiration)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _ownsCache = ownsCache;
            DefaultExpiration = defaultExpiration;
        }

        /// <summary>
        /// 无显式 TTL 时的绝对过期时长；默认 12 小时。&lt;=0 表示不过期。
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; }

        /// <inheritdoc />
        public void Add<V>(string key, V value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            TimeSpan? absolute = null;
            if (DefaultExpiration > TimeSpan.Zero)
                absolute = DefaultExpiration;
            SetEntry(key, value, absolute);
        }

        /// <inheritdoc />
        public void Add<V>(string key, V value, int cacheDurationInSeconds)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var seconds = cacheDurationInSeconds > 0 ? cacheDurationInSeconds : 1;
            SetEntry(key, value, TimeSpan.FromSeconds(seconds));
        }

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            if (_cache.Contains(key))
                return true;
            _keys.TryRemove(key, out _);
            return false;
        }

        /// <inheritdoc />
        public V Get<V>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default(V);
            var boxed = _cache.Get(key);
            if (boxed != null)
                return CastOrDefault<V>(boxed);
            _keys.TryRemove(key, out _);
            return default(V);
        }

        /// <inheritdoc />
        public V GetOrCreate<V>(string key, Func<V> factory)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            TimeSpan? absolute = null;
            if (DefaultExpiration > TimeSpan.Zero)
                absolute = DefaultExpiration;
            return GetOrCreateCore(key, factory, absolute);
        }

        /// <inheritdoc />
        public V GetOrCreate<V>(string key, Func<V> factory, int cacheDurationInSeconds)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var seconds = cacheDurationInSeconds > 0 ? cacheDurationInSeconds : 1;
            return GetOrCreateCore(key, factory, TimeSpan.FromSeconds(seconds));
        }

        /// <inheritdoc />
        public IEnumerable<string> GetKeys()
        {
            foreach (var key in _keys.Keys.ToArray())
            {
                if (!_cache.Contains(key))
                    _keys.TryRemove(key, out _);
            }
            return _keys.Keys.ToArray();
        }

        /// <inheritdoc />
        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_ownsCache)
                _cache.Dispose();
            _keys.Clear();
            _createLocks.Clear();
        }

        private V GetOrCreateCore<V>(string key, Func<V> factory, TimeSpan? absolute)
        {
            var existing = _cache.Get(key);
            if (existing != null)
                return CastOrDefault<V>(existing);

            var gate = _createLocks.GetOrAdd(key, _ => new object());
            lock (gate)
            {
                existing = _cache.Get(key);
                if (existing != null)
                    return CastOrDefault<V>(existing);

                var created = factory != null ? factory() : default(V);
                SetEntry(key, created, absolute);
                return created;
            }
        }

        private void SetEntry(string key, object value, TimeSpan? absolute)
        {
            var policy = new CacheItemPolicy
            {
                RemovedCallback = OnRemoved
            };
            if (absolute.HasValue && absolute.Value > TimeSpan.Zero)
                policy.AbsoluteExpiration = DateTimeOffset.Now.Add(absolute.Value);

            // System.Runtime.Caching.MemoryCache 不允许存 null
            _cache.Set(key, value ?? NullSentinel.Instance, policy);
            _keys[key] = 0;
        }

        private static V CastOrDefault<V>(object boxed)
        {
            if (ReferenceEquals(boxed, NullSentinel.Instance))
                return default(V);
            if (boxed is V typed)
                return typed;
            try
            {
                return (V)boxed;
            }
            catch
            {
                return default(V);
            }
        }

        private void OnRemoved(CacheEntryRemovedArguments args)
        {
            var s = args?.CacheItem?.Key;
            if (s != null)
            {
                _keys.TryRemove(s, out _);
                _createLocks.TryRemove(s, out _);
            }
        }

        private sealed class NullSentinel
        {
            public static readonly NullSentinel Instance = new NullSentinel();
        }
    }
}
