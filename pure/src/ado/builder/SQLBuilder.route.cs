using mooSQL.data.cluster;
using mooSQL.data.health;
using System;

namespace mooSQL.data
{
    public partial class SQLBuilder
    {
        private SQLRouteContext _pendingRouteContext;

        /// <summary>执行作用域路由上下文（读 Executor，写 pending/Executor）。</summary>
        public SQLRouteContext RouteContext
        {
            get => Executor?.RouteContext ?? _pendingRouteContext;
            internal set
            {
                if (Executor != null)
                    Executor.RouteContext = value;
                else
                    _pendingRouteContext = value;
            }
        }

        internal void SyncPendingRouteContext()
        {
            if (_pendingRouteContext == null || Executor == null) return;
            if (Executor.RouteContext == null)
                Executor.RouteContext = _pendingRouteContext;
            _pendingRouteContext = null;
        }

        internal DBExecutor EnsureExecutionExecutor()
        {
            CheckDB();
            SyncPendingRouteContext();
            if (Executor == null)
                Executor = new DBExecutor(DBLive);
            return Executor;
        }

        public SQLBuilder useReadReplica() => useRoute(r => r.PreferReadReplica = true);

        public SQLBuilder useMaster() => useRoute(r => r.ForceMaster = true);

        public SQLBuilder useDualWrite(params int[] slavePositions) =>
            useRoute(r =>
            {
                r.EnableDualWrite = true;
                r.DualWritePositions = slavePositions;
            });

        /// <summary>临时 Failover；启用后立即探活并在需要时选举，绑定 DBLive / TargetInstance。</summary>
        public SQLBuilder useFailover(FailoverMode mode)
        {
            useRoute(r => r.FailoverOverride = mode);
            ApplyProactiveFailoverForBuilder();
            return this;
        }

        public SQLBuilder useTarget(int position) =>
            useRoute(r => r.TargetPosition = position);

        public SQLBuilder useTarget(DBInstance instance) =>
            useRoute(r => r.TargetInstance = instance);

        public SQLBuilder useReadPolicy(ReadRoutePolicy policy) =>
            useRoute(r => r.ReadPolicyOverride = policy);

        public SQLBuilder useRoute(Action<SQLRouteContext> configure)
        {
            if (configure == null) return this;
            SQLRouteContext ctx;
            if (Executor != null)
                ctx = Executor.RouteContext ?? (Executor.RouteContext = new SQLRouteContext());
            else
                ctx = _pendingRouteContext ?? (_pendingRouteContext = new SQLRouteContext());
            configure(ctx);
            return this;
        }

        public SQLBuilder resetRoute()
        {
            _pendingRouteContext = null;
            if (Executor != null)
                Executor.RouteContext = null;
            return this;
        }

        internal void CloneRouteFrom(SQLBuilder source)
        {
            var src = source?.Executor?.RouteContext ?? source?._pendingRouteContext;
            if (src != null)
                _pendingRouteContext = src.Clone();
            Signal = source?.Signal;
        }

        private void ApplyProactiveFailoverForBuilder()
        {
            CheckDB();
            SyncPendingRouteContext();

            var client = MooClient ?? DBLive?.client;
            if (client == null || DBLive == null) return;

            var groupId = client.resolveGroupIdFor(DBLive);
            if (groupId < 0)
                groupId = position > -1 ? position : DBLive.config?.index ?? 0;

            var group = client.getGroupInternal(groupId);
            var ctx = RouteContext;
            var mode = client.ResolveFailoverMode(groupId, ctx, group);
            if (!MooClient.ShouldAttemptFailover(mode)) return;

            var h = DBLive.Health;
            if (h != null && h.Options.Enabled)
            {
                if (h.Status == DBHealthStatus.None || h.NeedsProbe())
                {
                    try { h.Probe(); } catch { /* best effort */ }
                }
            }

            if (DBLive.Health?.Status != DBHealthStatus.Unavailable
                && FailoverPolicy.IsInstanceHealthy(DBLive))
            {
                BindWriteTarget(DBLive);
                return;
            }

            var elected = client.electIfUnavailable(groupId, DBLive, ctx?.FailoverElector, "builderProactive");
            if (elected != null)
                BindWriteTarget(elected);
        }

        private void BindWriteTarget(DBInstance instance)
        {
            if (instance == null) return;
            DBLive = instance;
            if (Executor != null)
                Executor.DBLive = instance;
            useRoute(r =>
            {
                r.TargetInstance = instance;
                r.WriteTarget = instance;
            });
        }
    }
}
