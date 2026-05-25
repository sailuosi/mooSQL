using mooSQL.data.cluster;
using mooSQL.data.context;
using mooSQL.data.health;
using System;

namespace mooSQL.data
{
    public partial class DBExecutor
    {
        /// <summary>强制使用 Unavailable 实例。</summary>
        public bool ForceUseUnavailable { get; set; }

        /// <summary>双写 fan-out 时跳过异步 ModifyMediator 复制。</summary>
        public bool SkipAsyncReplication { get; set; }

        /// <summary>探活等内部调用时跳过健康门禁。</summary>
        public bool SkipHealthCheck { get; set; }

        /// <summary>Builder 传入的临时路由上下文。</summary>
        public SQLRouteContext RouteContext { get; set; }

        private void EnsureHealthBeforeExecute()
        {
            if (SkipHealthCheck) return;
            var h = DBLive?.Health;
            if (h == null || !h.Options.Enabled) return;
            if (h.Status == DBHealthStatus.Unavailable && !ForceUseUnavailable)
                throw new DBUnavailableException(DBLive, "数据库实例当前不可用。");
            if (h.NeedsProbe())
            {
                try { h.Probe(); } catch { /* best effort */ }
            }
        }

        private void MarkHealthSuccess()
        {
            DBLive?.Health?.MarkSuccess();
        }

        private void MarkHealthFailure(Exception e)
        {
            if (ConnectionExceptionClassifier.IsConnectionError(e))
                DBLive?.Health?.MarkFailure(e);
        }

        private bool TryImmediateFailoverAndRetry<R>(SQLCmd sql, Func<ICmdExecutor, ExeContext, R> executor, Exception original, out R result)
        {
            result = default;
            if (session != null) return false;
            var client = DBLive?.client;
            if (client == null) return false;
            var pos = DBLive.config?.index ?? 0;
            var mode = RouteContext?.FailoverOverride ?? client.getGroupInternal(pos)?.FailoverMode ?? FailoverMode.Disabled;
            if (mode != FailoverMode.ImmediateOnFailure) return false;
            if (!ConnectionExceptionClassifier.IsConnectionError(original)) return false;

            MarkHealthFailure(original);
            var newMaster = client.electIfUnavailable(pos, DBLive, RouteContext?.FailoverElector, "immediate");
            if (newMaster == null) return false;

            DBLive = newMaster;
            Context = null;
            try
            {
                result = ExecuteCmd(sql, executor);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
