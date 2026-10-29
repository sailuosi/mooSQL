///////////////////////////////////////////////////////////
//  MirrorKeysRunner.cs
//  补齐源有目标无的行，再删除目标有源无的主键（删除镜像）
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace mooSQL.data.sync
{
    internal static class MirrorKeysRunner
    {
        public static bool Run<TSrc, TDst>(SyncTableJob<TSrc, TDst> job, DataSyncHost host)
            where TSrc : class, new()
            where TDst : class, new()
        {
            var typeName = typeof(TSrc).Name;
            if (!host.Options.AllowDestructiveReplace)
            {
                host.AppendProgress("ERR " + typeName + ": MirrorKeys 需要 AllowDestructiveReplace=true");
                return true;
            }

            // 模式 2 自定义 SQL 不做全表键镜像（范围不明）
            if (job.ResolvePullMode() == DataSyncPullMode.FromSql)
            {
                host.AppendProgress("ERR " + typeName + ": MirrorKeys 不支持 fromSql；请用实体键集或水位拉源");
                return true;
            }

            try
            {
                var srcType = typeof(TSrc);
                var dstType = typeof(TDst);
                var tableName = SyncEntityMeta.ResolveTableName(dstType);
                var (pkPropSrc, pkColSrc) = SyncEntityMeta.ResolvePrimaryKey(srcType, job.KeyColumn);
                var (pkPropDst, pkColDst) = typeof(TSrc) == typeof(TDst)
                    ? (pkPropSrc, pkColSrc)
                    : SyncEntityMeta.ResolvePrimaryKey(dstType, job.KeyColumn);

                SyncEntityMeta.EnsureSafeIdent(tableName);
                SyncEntityMeta.EnsureSafeIdent(pkColSrc);
                SyncEntityMeta.EnsureSafeIdent(pkColDst);

                var srcDb = SyncJobHelpers.Db(host, SyncJobHelpers.SourcePos(job, host));
                var dstDb = SyncJobHelpers.Db(host, SyncJobHelpers.TargetPos(job, host));
                if (host.Options.EnsureTargetSchema)
                    dstDb.ensureSchema<TDst>(host.Options.SchemaMode);

                var batchSize = SyncJobHelpers.EffectiveBatchSize(job, host);
                var srcRepo = srcDb.useRepo<TSrc>();
                var dstRepo = dstDb.useRepo<TDst>();
                SyncJobHelpers.ApplyBulkOptions(dstRepo, host);

                var targetFilter = job.OnTargetQuery ?? job.OnSourceQuery;
                var sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // —— 阶段 1：键集拉源，补齐缺失行，同时收集源键 ——
                object? srcCursor = null;
                long pulled = 0, inserted = 0, skipped = 0;

                while (true)
                {
                    if (!host.WaitIfPausedOrThrow())
                        break;

                    var after = srcCursor;
                    List<TSrc> batch;
                    try
                    {
                        batch = srcRepo.GetList(kit =>
                        {
                            SyncJobHelpers.ApplyKeysetPage(kit, pkColSrc, after, batchSize, job.OnSourceQuery);
                        });
                    }
                    catch (Exception ex)
                    {
                        return SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/mirror-pull");
                    }

                    if (batch == null || batch.Count == 0)
                        break;

                    pulled += batch.Count;
                    foreach (var row in batch)
                    {
                        var pk = pkPropSrc.GetValue(row)?.ToString();
                        if (!string.IsNullOrEmpty(pk))
                            sourceKeys.Add(pk);
                    }

                    List<TDst> mapped;
                    try
                    {
                        mapped = SyncJobHelpers.MapRows(job, batch);
                    }
                    catch (Exception ex)
                    {
                        srcCursor = SyncJobHelpers.GetPkValue(pkPropSrc, batch[batch.Count - 1]!);
                        if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/mirror-map"))
                            return true;
                        SyncJobHelpers.DelayBatch(host);
                        continue;
                    }

                    skipped += batch.Count - mapped.Count;

                    try
                    {
                        if (mapped.Count > 0)
                        {
                            var pkValues = new List<string>(mapped.Count);
                            foreach (var row in mapped)
                            {
                                var pk = pkPropDst.GetValue(row)?.ToString();
                                if (!string.IsNullOrEmpty(pk))
                                    pkValues.Add(pk);
                            }

                            var existing = SyncJobHelpers.FindExistingPks(
                                dstDb, tableName, pkColDst, pkPropDst, pkValues);

                            var toInsert = new List<TDst>(mapped.Count);
                            foreach (var row in mapped)
                            {
                                var pk = pkPropDst.GetValue(row)?.ToString();
                                if (string.IsNullOrEmpty(pk) || existing.Contains(pk))
                                {
                                    skipped++;
                                    continue;
                                }
                                toInsert.Add(row);
                            }

                            if (toInsert.Count > 0)
                                inserted += dstRepo.InsertRange(toInsert);
                        }
                    }
                    catch (Exception ex)
                    {
                        srcCursor = SyncJobHelpers.GetPkValue(pkPropSrc, batch[batch.Count - 1]!);
                        if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/mirror-write"))
                            return true;
                        SyncJobHelpers.DelayBatch(host);
                        continue;
                    }

                    srcCursor = SyncJobHelpers.GetPkValue(pkPropSrc, batch[batch.Count - 1]!);
                    if (batch.Count < batchSize)
                        break;
                    SyncJobHelpers.DelayBatch(host);
                }

                // —— 阶段 2：键集扫目标主键，删除源中不存在的 ——
                object? dstCursor = null;
                long deleted = 0;

                while (true)
                {
                    if (!host.WaitIfPausedOrThrow())
                        break;

                    var after = dstCursor;
                    List<string> targetPks;
                    object? lastTyped = null;
                    try
                    {
                        targetPks = LoadTargetPkPage(
                            dstDb, tableName, pkColDst, pkPropDst, after, batchSize, targetFilter, out lastTyped);
                    }
                    catch (Exception ex)
                    {
                        return SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/mirror-scan");
                    }

                    if (targetPks == null || targetPks.Count == 0)
                        break;

                    var orphans = new List<string>();
                    foreach (var pk in targetPks)
                    {
                        if (!sourceKeys.Contains(pk))
                            orphans.Add(pk);
                    }

                    try
                    {
                        if (orphans.Count > 0)
                            deleted += SyncJobHelpers.DeleteByPkValues(
                                dstDb, tableName, pkColDst, pkPropDst, orphans);
                    }
                    catch (Exception ex)
                    {
                        dstCursor = lastTyped;
                        if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/mirror-delete"))
                            return true;
                        SyncJobHelpers.DelayBatch(host);
                        continue;
                    }

                    dstCursor = lastTyped;
                    if (targetPks.Count < batchSize)
                        break;
                    SyncJobHelpers.DelayBatch(host);
                }

                host.AppendProgress(
                    "OK " + typeName + "/" + tableName
                    + ": mirror pulled=" + pulled
                    + ", inserted=" + inserted
                    + ", skipped=" + skipped
                    + ", deleted=" + deleted
                    + ", sourceKeys=" + sourceKeys.Count);
                return false;
            }
            catch (Exception ex)
            {
                return SyncJobHelpers.HandleBatchError(host, job, ex, typeName);
            }
        }

        static List<string> LoadTargetPkPage(
            DBInstance dstDb,
            string tableName,
            string pkCol,
            PropertyInfo pkProp,
            object? afterPk,
            int pageSize,
            Action<SQLBuilder>? extraWhere,
            out object? lastTypedPk)
        {
            lastTypedPk = null;
            var kit = dstDb.useSQL().select(pkCol).from(tableName);
            SyncJobHelpers.ApplyKeysetPage(kit, pkCol, afterPk, pageSize, extraWhere);
            var dt = kit.query();
            var list = new List<string>();
            if (dt == null || dt.Rows.Count == 0)
                return list;

            foreach (DataRow row in dt.Rows)
            {
                var raw = row[0];
                if (raw == null || raw == DBNull.Value)
                    continue;
                lastTypedPk = raw;
                var s = raw.ToString();
                if (!string.IsNullOrEmpty(s))
                    list.Add(s);
            }

            // 若类型与属性不一致，尽量转成属性类型值作下一游标
            if (lastTypedPk != null && pkProp != null)
            {
                try
                {
                    var t = Nullable.GetUnderlyingType(pkProp.PropertyType) ?? pkProp.PropertyType;
                    if (lastTypedPk.GetType() != t)
                        lastTypedPk = Convert.ChangeType(lastTypedPk, t);
                }
                catch
                {
                    // 保留原值
                }
            }

            return list;
        }
    }
}
