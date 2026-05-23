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
            var pos = anchor.config?.index ?? 0;
            var queryType = sql?.type ?? QueryType.Unknown;
            var isWrite = RouteOperation.IsWrite(queryType, ctx);

            if (ctx?.TargetInstance != null) return ctx.TargetInstance;
            if (ctx?.TargetPosition != null)
                return client.CashHolder?.getInstance(ctx.TargetPosition.Value) ?? anchor;

            if (ctx?.ForceMaster == true)
                return resolver.ResolveWrite(pos, ctx) ?? anchor;

            if (!isWrite)
            {
                if (ctx?.PreferReadReplica == true || resolver.GetGroup(pos) != null)
                    return resolver.ResolveRead(pos, ctx) ?? anchor;
                return anchor;
            }

            return resolver.ResolveWrite(pos, ctx) ?? anchor;
        }

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
