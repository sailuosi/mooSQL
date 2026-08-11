using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace mooSQL.data
{
    /// <summary>
    /// 基于 <see cref="IMemoryCache"/> 的 <see cref="ISooCache"/> 实现。
    /// 无显式 TTL 时使用 <see cref="DefaultExpiration"/>（默认 12 小时）。
    /// 示例：<c>client.useCache(new MemorySooCache(TimeSpan.FromHours(6)));</c>
    /// </summary>
    public class MemorySooCache : ISooCache, IDisposable
    {
        private readonly IMemoryCache _cache;
        private readonly bool _ownsCache;
        private readonly ConcurrentDictionary<string, byte> _keys = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

        /// <summary>使用默认 <see cref="MemoryCache"/>，默认过期 12 小时。</summary>
        public MemorySooCache()
            : this(SooCacheDefaults.Expiration)
        {
        }

        /// <summary>使用默认 <see cref="MemoryCache"/>，并指定无显式 TTL 时的默认过期。</summary>
        public MemorySooCache(TimeSpan defaultExpiration)
            : this(new MemoryCache(new MemoryCacheOptions()), ownsCache: true, defaultExpiration)
        {
        }

        /// <summary>使用默认 <see cref="MemoryCache"/>，并指定无显式 TTL 时的默认过期秒数。</summary>
        public MemorySooCache(int defaultExpirationSeconds)
            : this(TimeSpan.FromSeconds(defaultExpirationSeconds))
        {
        }

        /// <summary>注入外部 <see cref="IMemoryCache"/>（不负责 Dispose），默认过期 12 小时。</summary>
        public MemorySooCache(IMemoryCache cache)
            : this(cache, ownsCache: false, SooCacheDefaults.Expiration)
        {
        }

        /// <summary>注入外部 <see cref="IMemoryCache"/>，并指定默认过期。</summary>
        public MemorySooCache(IMemoryCache cache, TimeSpan defaultExpiration)
            : this(cache, ownsCache: false, defaultExpiration)
        {
        }

        private MemorySooCache(IMemoryCache cache, bool ownsCache, TimeSpan defaultExpiration)
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
            var options = BuildOptions(absolute);
            _cache.Set(key, (object)value, options);
            _keys[key] = 0;
        }

        /// <inheritdoc />
        public void Add<V>(string key, V value, int cacheDurationInSeconds)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var seconds = cacheDurationInSeconds > 0 ? cacheDurationInSeconds : 1;
            var options = BuildOptions(TimeSpan.FromSeconds(seconds));
            _cache.Set(key, (object)value, options);
            _keys[key] = 0;
        }

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            if (_cache.TryGetValue(key, out _))
                return true;
            _keys.TryRemove(key, out _);
            return false;
        }

        /// <inheritdoc />
        public V Get<V>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default(V);
            object boxed;
            if (_cache.TryGetValue(key, out boxed))
            {
                if (boxed is V typed)
                    return typed;
                if (boxed != null)
                {
                    try
                    {
                        return (V)boxed;
                    }
                    catch
                    {
                        return default(V);
                    }
                }
                return default(V);
            }
            _keys.TryRemove(key, out _);
            return default(V);
        }

        /// <inheritdoc />
        public V GetOrCreate<V>(string key, Func<V> factory)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _cache.GetOrCreate(key, entry =>
            {
                if (DefaultExpiration > TimeSpan.Zero)
                    entry.AbsoluteExpirationRelativeToNow = DefaultExpiration;
                entry.RegisterPostEvictionCallback(OnEvicted);
                _keys[key] = 0;
                return factory != null ? factory() : default(V);
            });
        }

        /// <inheritdoc />
        public V GetOrCreate<V>(string key, Func<V> factory, int cacheDurationInSeconds)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var seconds = cacheDurationInSeconds > 0 ? cacheDurationInSeconds : 1;
            return _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(seconds);
                entry.RegisterPostEvictionCallback(OnEvicted);
                _keys[key] = 0;
                return factory != null ? factory() : default(V);
            });
        }

        /// <inheritdoc />
        public IEnumerable<string> GetKeys()
        {
            foreach (var key in _keys.Keys.ToArray())
            {
                if (!_cache.TryGetValue(key, out _))
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
        }

        private MemoryCacheEntryOptions BuildOptions(TimeSpan? absolute)
        {
            var options = new MemoryCacheEntryOptions();
            if (absolute.HasValue)
                options.AbsoluteExpirationRelativeToNow = absolute.Value;
            options.RegisterPostEvictionCallback(OnEvicted);
            return options;
        }

        private void OnEvicted(object key, object value, EvictionReason reason, object state)
        {
            var s = key as string;
            if (s != null)
                _keys.TryRemove(s, out _);
        }
    }
}
