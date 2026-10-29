///////////////////////////////////////////////////////////
//  AppendOnlyRunner.cs
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Reflection;

namespace mooSQL.data.sync
{
    /// <summary>AppendOnly：水位拉批 + PK 探活 + InsertRange。</summary>
    internal static class AppendOnlyRunner
    {
        public static bool Run<TSrc, TDst>(SyncTableJob<TSrc, TDst> job, SooDataSyncHost host)
            where TSrc : class, new()
            where TDst : class, new()
        {
            var pull = job.ResolvePullMode();
            if (pull == SooDataSyncPullMode.FromSql || pull == SooDataSyncPullMode.EntityByPk)
                return PageAppendRunner.Run(job, host, probePk: true);

            return WatermarkAppend(job, host, probePk: true);
        }

        /// <param name="ignoreWatermark">true 时不读目标 max，从源全量按序拉（ReplaceFull）。</param>
        internal static bool WatermarkAppend<TSrc, TDst>(
            SyncTableJob<TSrc, TDst> job,
            SooDataSyncHost host,
            bool probePk,
            bool ignoreWatermark = false)
            where TSrc : class, new()
            where TDst : class, new()
        {
            var typeName = typeof(TSrc).Name;
            try
            {
                if (string.IsNullOrWhiteSpace(job.OrderByColumn))
                    throw new InvalidOperationException("AppendOnly 水位模式需要 orderBy");

                SyncEntityMeta.EnsureSafeIdent(job.OrderByColumn);

                var srcType = typeof(TSrc);
                var dstType = typeof(TDst);
                var tableName = SyncEntityMeta.ResolveTableName(dstType);
                var (orderProp, orderCol) = SyncEntityMeta.ResolveOrderBy(srcType, job.OrderByColumn);
                var (pkPropSrc, pkColSrc) = SyncEntityMeta.ResolvePrimaryKey(srcType, job.KeyColumn);
                var (pkPropDst, pkColDst) = typeof(TSrc) == typeof(TDst)
                    ? (pkPropSrc, pkColSrc)
                    : SyncEntityMeta.ResolvePrimaryKey(dstType, job.KeyColumn);

                var wmCol = string.IsNullOrWhiteSpace(job.WatermarkColumnOnTarget)
                    ? orderCol
                    : job.WatermarkColumnOnTarget;

                SyncEntityMeta.EnsureSafeIdent(orderCol);
                SyncEntityMeta.EnsureSafeIdent(pkColSrc);
                SyncEntityMeta.EnsureSafeIdent(pkColDst);
                SyncEntityMeta.EnsureSafeIdent(tableName);
                SyncEntityMeta.EnsureSafeIdent(wmCol);

                var srcDb = SyncJobHelpers.SourceDb(job, host);
                var dstDb = SyncJobHelpers.TargetDb(job, host);

                if (host.Options.EnsureTargetSchema)
                    dstDb.ensureSchema<TDst>(host.Options.SchemaMode);

                var batchSize = SyncJobHelpers.EffectiveBatchSize(job, host);
                DateTime? watermark = null;
                if (!ignoreWatermark)
                {
                    watermark = dstDb.useSQL()
                        .select("max(" + wmCol + ")")
                        .from(tableName)
                        .queryScalar<DateTime?>();
                }

                DateTime? cursorTime = watermark;
                var cursorPk = string.Empty;

                var srcRepo = srcDb.useRepo<TSrc>();
                var dstRepo = dstDb.useRepo<TDst>();
                SyncJobHelpers.ApplyBulkOptions(dstRepo, host);

                long pulled = 0, inserted = 0, skipped = 0;

                while (true)
                {
                    if (!host.WaitIfPausedOrThrow())
                        break;

                    List<TSrc> batch;
                    try
                    {
                        var ct = cursorTime;
                        var cpk = cursorPk;
                        batch = srcRepo.GetList(kit =>
                        {
                            kit.whereIsNotNull(orderCol);
                            if (ct.HasValue)
                            {
                                kit.whereFormat(
                                    "((" + orderCol + " > {0}) or (" + orderCol + " = {0} and " + pkColSrc + " > {1}))",
                                    ct.Value,
                                    cpk ?? string.Empty);
                            }

                            job.OnSourceQuery?.Invoke(kit);

                            if (kit.current == null || kit.current.orderPart == null || kit.current.orderPart.Count == 0)
                                kit.orderBy(orderCol + " asc, " + pkColSrc + " asc");
                            kit.top(batchSize);
                        });
                    }
                    catch (Exception ex)
                    {
                        return SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/pull");
                    }

                    if (batch == null || batch.Count == 0)
                        break;

                    pulled += batch.Count;

                    List<TDst> mapped;
                    try
                    {
                        mapped = SyncJobHelpers.MapRows(job, batch);
                    }
                    catch (Exception ex)
                    {
                        AdvanceCursor(batch, orderProp, pkPropSrc, ref cursorTime, ref cursorPk);
                        if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/map"))
                            return true;
                        SyncJobHelpers.DelayBatch(host);
                        continue;
                    }

                    skipped += batch.Count - mapped.Count;

                    try
                    {
                        if (probePk && mapped.Count > 0)
                        {
                            var pkValues = new List<string>(mapped.Count);
                            foreach (var row in mapped)
                            {
                                var pk = pkPropDst.GetValue(row)?.ToString();
                                if (!string.IsNullOrEmpty(pk))
                                    pkValues.Add(pk);
                            }

                            // 始终探活：水位列全 NULL 时 max 也为 null，不可当作空表
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
                        else if (mapped.Count > 0)
                        {
                            inserted += dstRepo.InsertRange(mapped);
                        }
                    }
                    catch (Exception ex)
                    {
                        AdvanceCursor(batch, orderProp, pkPropSrc, ref cursorTime, ref cursorPk);
                        if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/write"))
                            return true;
                        SyncJobHelpers.DelayBatch(host);
                        continue;
                    }

                    AdvanceCursor(batch, orderProp, pkPropSrc, ref cursorTime, ref cursorPk);

                    if (batch.Count < batchSize)
                        break;

                    SyncJobHelpers.DelayBatch(host);
                }

                var wmText = cursorTime.HasValue
                    ? cursorTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")
                    : "(empty)";
                host.AppendProgress(
                    "OK " + typeName + "/" + tableName
                    + ": pulled=" + pulled + ", inserted=" + inserted
                    + ", skipped=" + skipped + ", watermark=" + wmText);
                return false;
            }
            catch (Exception ex)
            {
                return SyncJobHelpers.HandleBatchError(host, job, ex, typeName);
            }
        }

        static void AdvanceCursor<TSrc>(
            IList<TSrc> batch,
            PropertyInfo orderProp,
            PropertyInfo pkProp,
            ref DateTime? cursorTime,
            ref string cursorPk)
        {
            if (batch == null || batch.Count == 0)
                return;
            var last = batch[batch.Count - 1];
            cursorTime = orderProp.GetValue(last) as DateTime?;
            cursorPk = pkProp.GetValue(last)?.ToString() ?? string.Empty;
        }
    }
}
