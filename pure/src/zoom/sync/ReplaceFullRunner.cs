///////////////////////////////////////////////////////////
//  ReplaceFullRunner.cs
//  先清空再全量批拉 + Bulk
///////////////////////////////////////////////////////////

using System;

namespace mooSQL.data.sync
{
    internal static class ReplaceFullRunner
    {
        public static bool Run<TSrc, TDst>(SyncTableJob<TSrc, TDst> job, DataSyncHost host)
            where TSrc : class, new()
            where TDst : class, new()
        {
            var typeName = typeof(TSrc).Name;
            if (!host.Options.AllowDestructiveReplace)
            {
                host.AppendProgress("ERR " + typeName + ": ReplaceFull 需要 AllowDestructiveReplace=true");
                return true;
            }

            try
            {
                var tableName = SyncEntityMeta.ResolveTableName(typeof(TDst));
                SyncEntityMeta.EnsureSafeIdent(tableName);
                var dstDb = SyncJobHelpers.Db(host, SyncJobHelpers.TargetPos(job, host));

                if (host.Options.EnsureTargetSchema)
                    dstDb.ensureSchema<TDst>(host.Options.SchemaMode);

                var pull = job.ResolvePullMode();
                if (!ClearTarget(job, host, dstDb, tableName, pull, typeName))
                    return true;

                // 目标已空 → 水位 max 为 null → WatermarkAppend 即全量
                if (pull == DataSyncPullMode.Watermark)
                    return AppendOnlyRunner.WatermarkAppend(job, host, probePk: false);
                return PageAppendRunner.Run(job, host, probePk: false);
            }
            catch (Exception ex)
            {
                return SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/ReplaceFull");
            }
        }

        static bool ClearTarget<TSrc, TDst>(
            SyncTableJob<TSrc, TDst> job,
            DataSyncHost host,
            DBInstance dstDb,
            string tableName,
            DataSyncPullMode pull,
            string typeName)
            where TSrc : class, new()
            where TDst : class, new()
        {
            // 模式 2：禁止静默整表 DELETE；须 onTargetClear 限定窗口
            if (pull == DataSyncPullMode.FromSql)
            {
                if (job.OnTargetClear == null)
                {
                    host.AppendProgress(
                        "ERR " + typeName + ": ReplaceFull+fromSql 必须配置 onTargetClear（窗口 DELETE），禁止误清整表");
                    return false;
                }

                var kit = dstDb.useSQL().from(tableName);
                job.OnTargetClear(kit);
                if (kit.current == null || kit.current.wherePart == null || kit.current.wherePart.Count == 0)
                {
                    host.AppendProgress("ERR " + typeName + ": onTargetClear 未设置 where，拒绝执行");
                    return false;
                }

                var n = kit.doDelete();
                host.AppendProgress("ReplaceFull window-cleared " + tableName + " rows~=" + n);
                return true;
            }

            if (job.OnTargetClear != null)
            {
                var kit = dstDb.useSQL().from(tableName);
                job.OnTargetClear(kit);
                if (kit.current == null || kit.current.wherePart == null || kit.current.wherePart.Count == 0)
                {
                    host.AppendProgress("ERR " + typeName + ": onTargetClear 未设置 where，拒绝执行");
                    return false;
                }
                var n = kit.doDelete();
                host.AppendProgress("ReplaceFull cleared-by-hook " + tableName + " rows~=" + n);
                return true;
            }

            // 水位 / 主键翻页：整表 DELETE
            dstDb.useSQL().from(tableName).where("1=1").doDelete();
            host.AppendProgress("ReplaceFull cleared " + tableName);
            return true;
        }
    }
}
