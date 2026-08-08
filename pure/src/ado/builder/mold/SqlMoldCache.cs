using System;
using mooSQL.data.clip;

namespace mooSQL.data
{
    /// <summary>
    /// SQLMold 全局缓存（按 PathKey）。
    /// </summary>
    public static class SqlMoldCache
    {
        static readonly object Gate = new object();
        static FrequencyBasedCache<SqlMoldPathKey, SQLMold> _cache;

        /// <summary>默认过期 30 分钟。</summary>
        public static TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(30);

        static FrequencyBasedCache<SqlMoldPathKey, SQLMold> Cache
        {
            get
            {
                if (_cache != null) return _cache;
                lock (Gate)
                {
                    if (_cache == null)
                        _cache = new FrequencyBasedCache<SqlMoldPathKey, SQLMold>(Expiration);
                    return _cache;
                }
            }
        }

        /// <summary>尝试获取只读模版。</summary>
        public static bool TryGet(SqlMoldPathKey key, out SQLMold mold)
        {
            mold = null;
            if (key == null) return false;
            return Cache.TryGetValue(key, out mold) && mold != null;
        }

        /// <summary>写入模版（覆盖同键）。</summary>
        public static void Set(SqlMoldPathKey key, SQLMold mold)
        {
            if (key == null || mold == null) return;
            Cache.Add(key, mold);
        }

        /// <summary>测试用：替换缓存实例。</summary>
        public static void ResetForTests()
        {
            lock (Gate)
            {
                _cache = new FrequencyBasedCache<SqlMoldPathKey, SQLMold>(Expiration);
            }
        }
    }
}
