using mooSQL.data.cluster;
using System;

namespace mooSQL.data
{
    public partial class SQLBuilder
    {
        public SQLRouteContext RouteContext;

        public SQLBuilder useReadReplica() => useRoute(r => r.PreferReadReplica = true);

        public SQLBuilder useMaster() => useRoute(r => r.ForceMaster = true);

        public SQLBuilder useDualWrite(params int[] slavePositions) =>
            useRoute(r =>
            {
                r.EnableDualWrite = true;
                r.DualWritePositions = slavePositions;
            });

        public SQLBuilder useFailover(FailoverMode mode) =>
            useRoute(r => r.FailoverOverride = mode);

        public SQLBuilder useTarget(int position) =>
            useRoute(r => r.TargetPosition = position);

        public SQLBuilder useTarget(DBInstance instance) =>
            useRoute(r => r.TargetInstance = instance);

        public SQLBuilder useReadPolicy(ReadRoutePolicy policy) =>
            useRoute(r => r.ReadPolicyOverride = policy);

        public SQLBuilder useRoute(Action<SQLRouteContext> configure)
        {
            if (configure == null) return this;
            if (RouteContext == null) RouteContext = new SQLRouteContext();
            configure(RouteContext);
            return this;
        }

        public SQLBuilder resetRoute()
        {
            RouteContext = null;
            return this;
        }

        internal void CloneRouteFrom(SQLBuilder source)
        {
            RouteContext = source?.RouteContext?.Clone();
            Signal = source?.Signal;
        }

        internal void ResolveRouteBeforeExecute(bool isWrite)
        {
            if (RouteContext == null) return;
            var cash = MooClient?.CashHolder;
            if (cash == null) return;

            var ctx = RouteContext ?? new SQLRouteContext();
            ctx.IsWriteOperation = isWrite;
            var pos = position > -1 ? position : (DBLive.config?.index ?? 0);

            if (ctx.TargetInstance != null)
            {
                setDBInstance(ctx.TargetInstance);
                return;
            }
            if (ctx.TargetPosition.HasValue)
            {
                position = ctx.TargetPosition.Value;
                setDBInstance(cash.getInstance(position));
                return;
            }
            if (ctx.ForceMaster == true)
            {
                setDBInstance(cash.resolveWrite(pos, RouteContext));
                return;
            }
            if (!isWrite && ctx.PreferReadReplica == true)
            {
                setDBInstance(cash.resolveRead(pos, RouteContext));
                return;
            }
            if (isWrite && ctx.EnableDualWrite == true)
            {
                setDBInstance(cash.resolveWrite(pos, RouteContext));
                return;
            }
            if (ctx.FailoverOverride.HasValue && isWrite)
            {
                setDBInstance(cash.resolveWrite(pos, RouteContext));
            }
        }
    }
}
