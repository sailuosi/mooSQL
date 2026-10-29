///////////////////////////////////////////////////////////
//  ReplaceFullRunner.cs
//  先清空再全量批拉 + Bulk
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

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

                // 整表 DELETE 清空（where 不可空）；窗口范围 DELETE 后置增强
                dstDb.useSQL().from(tableName).where("1=1").doDelete();
                host.AppendProgress("ReplaceFull cleared " + tableName);

                var pull = job.ResolvePullMode();
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
    }
}
