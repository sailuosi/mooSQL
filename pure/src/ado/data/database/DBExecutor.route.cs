using mooSQL.data.cluster;
using mooSQL.data.model;

namespace mooSQL.data
{
    public partial class DBExecutor
    {
        private DBInstance _routingRestoreDbLive;

        private bool HasActiveTransaction => session != null;

        internal DBInstance ResolveTargetInstance(SQLCmd sql)
        {
            var anchor = DBLive;
            if (anchor == null) return null;

            if (HasActiveTransaction)
                return DBLive;

            var client = anchor.client;
            var resolver = client?.GetRouteResolver();
            if (resolver == null) return anchor;

            var ctx = RouteContext;
            var groupId = client.resolveGroupIdFor(anchor);
            if (groupId < 0) groupId = anchor.config?.index ?? 0;
            var queryType = sql?.type ?? QueryType.Unknown;
            var isWrite = RouteOperation.IsWrite(queryType, ctx);

            if (ctx?.TargetInstance != null) return ctx.TargetInstance;
            if (ctx?.TargetPosition != null)
                return client.CashHolder?.getInstance(ctx.TargetPosition.Value) ?? anchor;

            if (ctx?.ForceMaster == true)
                return resolver.ResolveWrite(groupId, ctx, anchor) ?? anchor;

            if (!isWrite)
            {
                if (resolver.ShouldAutoReadReplica(groupId, ctx))
                    return resolver.ResolveRead(groupId, ctx) ?? anchor;
                return anchor;
            }

            return resolver.ResolveWrite(groupId, ctx, anchor) ?? anchor;
        }

        /// <summary>当次执行临时切换 DBLive，finally 中 <see cref="EndExecutionRouting"/> 恢复锚点（与 useFailover 永久改绑不同）。</summary>
        private void BeginExecutionRouting(SQLCmd sql)
        {
            _routingRestoreDbLive = DBLive;
            var target = ResolveTargetInstance(sql);
            if (target != null && target != DBLive)
            {
                DBLive = target;
                if (Context != null)
                    Context.DBLive = target;
            }
        }

        private void EndExecutionRouting()
        {
            if (_routingRestoreDbLive != null && DBLive != _routingRestoreDbLive)
            {
                DBLive = _routingRestoreDbLive;
                if (Context != null)
                    Context.DBLive = _routingRestoreDbLive;
            }
            _routingRestoreDbLive = null;
        }
    }

    internal static class RouteOperation
    {
        public static bool IsWrite(QueryType type, SQLRouteContext ctx)
        {
            if (ctx?.ForceMaster == true) return true;
            if (type == QueryType.Select) return false;
            if (type == QueryType.Unknown) return true;
            return IsWriteType(type);
        }

        public static bool IsWriteType(QueryType type)
        {
            switch (type)
            {
                case QueryType.Insert:
                case QueryType.Update:
                case QueryType.Delete:
                case QueryType.Merge:
                case QueryType.InsertOrUpdate:
                case QueryType.MultiInsert:
                case QueryType.CreateTable:
                case QueryType.DropTable:
                case QueryType.TruncateTable:
                case QueryType.Composite:
                    return true;
                default:
                    return false;
            }
        }
    }
}
