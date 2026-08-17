using System;
using System.Threading.Tasks;
using mooSQL.data.context;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// SELECT 结果缓存：显式 <see cref="setCache(string, int)"/> / 自动指纹 <see cref="setCache(int)"/>。
    /// 仅经 <c>doSelect</c> / <c>doSelectAsync</c> 进入，不面向写命令。
    /// </summary>
    public partial class StepBuilder
    {
        /// <summary>是否已启用结果缓存（任一 setCache 重载）。</summary>
        public bool resultCacheEnabled;

        /// <summary>用户显式键或已启用自动缓存。</summary>
        public bool IsResultCacheArmed
            => resultCacheEnabled || !string.IsNullOrWhiteSpace(cacheKey);

        /// <summary>是否存在业务显式缓存键。</summary>
        public bool HasUserResultCacheKey
            => !string.IsNullOrWhiteSpace(cacheKey);

        /// <summary>
        /// 仅 TTL：无外界 key，查询时用 <see cref="BuildAutoResultCacheKey"/>。
        /// </summary>
        public override SQLBuilder setCache(int timeoutSeconds)
        {
            this.cacheKey = "";
            this.cacheTimeout = timeoutSeconds > 0 ? timeoutSeconds : defaultCacheTimeout;
            this.resultCacheEnabled = true;
            return this;
        }

        string ResolveResultCacheDatabaseId()
        {
            try
            {
                if (DBLive?.config != null)
                    return DBLive.config.index.ToString();
            }
            catch
            {
                // ignore
            }
            return "";
        }

        /// <summary>显式用户键规范化：<c>RC:USER:{db}:{key}</c>（已以 RC: 开头则原样）。</summary>
        internal string NormalizeUserResultCacheKey(string userKey)
        {
            if (string.IsNullOrWhiteSpace(userKey))
                return userKey;
            if (userKey.StartsWith(SQLCmd.ResultCacheKeyPrefix, StringComparison.Ordinal))
                return userKey;
            return ResultCacheKey.ForUser(ResolveResultCacheDatabaseId(), userKey);
        }

        internal bool IsResultCacheInTransaction()
        {
            try
            {
                var session = Executor?.Context?.session;
                return session != null && session.transState == ExeSessionTransState.Executing;
            }
            catch
            {
                return false;
            }
        }

        internal bool IsResultCachePagingActive()
        {
            var g = current;
            if (g == null) return false;
            return g.skipNum > 0 || g.pageNum > 0;
        }

        /// <summary>事务中跳过；分页默认跳过（显式用户键可覆盖分页）。</summary>
        internal bool ShouldSkipResultCacheByPolicy(bool hasUserKey)
        {
            if (IsResultCacheInTransaction())
                return true;
            if (IsResultCachePagingActive() && !hasUserKey)
                return true;
            return false;
        }

        internal static bool IsSelectResultCacheCmd(SQLCmd cmd)
            => cmd != null && cmd.type == QueryType.Select;

        internal string ResolveResultCacheStorageKey(SQLCmd cmd, string resultTypeTag, bool hasUserKey)
        {
            string key;
            if (hasUserKey)
                key = NormalizeUserResultCacheKey(cacheKey);
            else
                key = BuildAutoResultCacheKey(cmd);

            if (!string.IsNullOrEmpty(resultTypeTag))
                key = key + ":" + resultTypeTag;
            return key;
        }

        internal bool TryGetResultCacheValue<T>(string storageKey, out T value)
        {
            value = default(T);
            if (string.IsNullOrEmpty(storageKey))
                return false;
            if (!cacheHolder.ContainsKey(storageKey))
                return false;
            value = cacheHolder.Get<T>(storageKey);
            return true;
        }

        internal void TryAddResultCacheValue<T>(string storageKey, T value)
        {
            if (string.IsNullOrEmpty(storageKey))
                return;
            var ttl = cacheTimeout > 0 ? cacheTimeout : defaultCacheTimeout;
            cacheHolder.Add(storageKey, value, ttl);
        }

        bool TryHitUserResultCache<T>(string resultTypeTag, out T hit)
        {
            hit = default(T);
            if (!IsResultCacheArmed || !HasUserResultCacheKey)
                return false;
            if (ShouldSkipResultCacheByPolicy(true))
                return false;
            var ukey = ResolveResultCacheStorageKey(null, resultTypeTag, true);
            return TryGetResultCacheValue(ukey, out hit);
        }

        /// <summary>
        /// SELECT 管线（默认 <see cref="toSelect"/>）：取 key → 命中返回；否则执行并可选写入。
        /// </summary>
        private T doSelect<T>(string resultTypeTag, Func<SQLCmd, T> onSelect) {
            var cmd = this.toSelect();
            if (cmd == null || string.IsNullOrEmpty(cmd.sql))
                return default(T);
            var res= doSelectCore(resultTypeTag,cmd, onSelect);
            doPrintSQL(cmd);
            return res;
        }
            

        /// <summary>SELECT 管线：已物化 Select cmd（ScriptTemplate 等）。</summary>
        private T doSelect<T>(string resultTypeTag, SQLCmd cmd, Func<SQLCmd, T> onSelect)
        {
            if (onSelect == null) throw new ArgumentNullException(nameof(onSelect));
            if (TryHitUserResultCache(resultTypeTag, out T userHit))
                return userHit;
            if (cmd == null || string.IsNullOrEmpty(cmd.sql))
                return default(T);
            return doSelectCore(resultTypeTag, cmd, onSelect);
        }



        T doSelectCore<T>(string resultTypeTag, SQLCmd cmd, Func<SQLCmd, T> onSelect)
        {
            var hasUserKey = HasUserResultCacheKey;
            if (!IsResultCacheArmed || ShouldSkipResultCacheByPolicy(hasUserKey))
                return FinishSelect(onSelect(cmd));

            cmd.EnsureLiveParasResolved();

            if (!IsSelectResultCacheCmd(cmd))
                return FinishSelect(onSelect(cmd));

            var skey = ResolveResultCacheStorageKey(cmd, resultTypeTag, hasUserKey);
            if (TryGetResultCacheValue(skey, out T hit))
                return hit;

            var res = onSelect(cmd);
            if (!ShouldSkipResultCacheByPolicy(hasUserKey))
                TryAddResultCacheValue(skey, res);
            return FinishSelect(res);
        }

        /// <summary>执行后按 <see cref="CleanWay.Always"/> 清理构造状态（缓存命中不清理）。</summary>
        T FinishSelect<T>(T res)
        {
            if (this._AutoClearWay == CleanWay.Always)
                clear();
            return res;
        }

        /// <summary>异步 SELECT（默认 <see cref="toSelect"/>）。缓存结果值而非 Task。</summary>
        private Task<T> doSelectAsync<T>(string resultTypeTag, Func<SQLCmd, Task<T>> onSelect) { 
            var cmd = this.toSelect();
            if (cmd == null || string.IsNullOrEmpty(cmd.sql))
                return Task.FromResult(default(T));
            var res = doSelectCore(resultTypeTag, cmd, onSelect);
            doPrintSQL(cmd);
            return res;
        }
        

        /// <summary>异步 SELECT：已物化 cmd。</summary>
        private Task<T> doSelectAsync<T>(string resultTypeTag, SQLCmd cmd, Func<SQLCmd, Task<T>> onSelect)
        {
            if (onSelect == null) throw new ArgumentNullException(nameof(onSelect));
            if (TryHitUserResultCache(resultTypeTag, out T userHit))
                return Task.FromResult(userHit);
            if (cmd == null || string.IsNullOrEmpty(cmd.sql))
                return Task.FromResult(default(T));
            return doSelectCoreAsync(resultTypeTag, cmd, onSelect);
        }


        async Task<T> doSelectCoreAsync<T>(string resultTypeTag, SQLCmd cmd, Func<SQLCmd, Task<T>> onSelect)
        {
            var hasUserKey = HasUserResultCacheKey;
            if (!IsResultCacheArmed || ShouldSkipResultCacheByPolicy(hasUserKey))
                return FinishSelect(await onSelect(cmd).ConfigureAwait(false));

            cmd.EnsureLiveParasResolved();

            if (!IsSelectResultCacheCmd(cmd))
                return FinishSelect(await onSelect(cmd).ConfigureAwait(false));

            var skey = ResolveResultCacheStorageKey(cmd, resultTypeTag, hasUserKey);
            if (TryGetResultCacheValue(skey, out T hit))
                return hit;

            var res = await onSelect(cmd).ConfigureAwait(false);
            if (!ShouldSkipResultCacheByPolicy(hasUserKey))
                TryAddResultCacheValue(skey, res);
            return FinishSelect(res);
        }
    }
}
