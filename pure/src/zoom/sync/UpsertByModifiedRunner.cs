///////////////////////////////////////////////////////////
//  UpsertByModifiedRunner.cs
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Reflection;

namespace mooSQL.data.sync
{
    /// <summary>按主键探活后 InsertRange / UpdateRange（修改时间前进才更新）。</summary>
    internal static class UpsertByModifiedRunner
    {
        public static bool Run<TSrc, TDst>(SyncTableJob<TSrc, TDst> job, DataSyncHost host)
            where TSrc : class, new()
            where TDst : class, new()
        {
            // 首版：水位拉批（或翻页）后 SaveRange 语义——存在则 Update，否则 Insert；
            // 精细 Modified 比较在目标行加载后判断。
            var pull = job.ResolvePullMode();
            if (pull != DataSyncPullMode.Watermark)
            {
                host.AppendProgress("ERR UpsertByModified 当前仅实现时序水位拉源，表=" + typeof(TSrc).Name);
                return SyncJobHelpers.Failure(job, host) == DataSyncFailureAction.Abort;
            }

            var typeName = typeof(TSrc).Name;
            try
            {
                var srcType = typeof(TSrc);
                var dstType = typeof(TDst);
                var tableName = SyncEntityMeta.ResolveTableName(dstType);
                var (orderProp, orderCol) = SyncEntityMeta.ResolveOrderBy(srcType, job.OrderByColumn);
                var (pkPropSrc, pkColSrc) = SyncEntityMeta.ResolvePrimaryKey(srcType);
                var (pkPropDst, pkColDst) = typeof(TSrc) == typeof(TDst)
                    ? (pkPropSrc, pkColSrc)
                    : SyncEntityMeta.ResolvePrimaryKey(dstType);

                SyncEntityMeta.EnsureSafeIdent(orderCol);
                SyncEntityMeta.EnsureSafeIdent(pkColDst);
                SyncEntityMeta.EnsureSafeIdent(tableName);

                var srcDb = SyncJobHelpers.Db(host, SyncJobHelpers.SourcePos(job, host));
                var dstDb = SyncJobHelpers.Db(host, SyncJobHelpers.TargetPos(job, host));
                if (host.Options.EnsureTargetSchema)
                    dstDb.ensureSchema<TDst>(host.Options.SchemaMode);

                var batchSize = SyncJobHelpers.EffectiveBatchSize(job, host);
                var wmCol = string.IsNullOrWhiteSpace(job.WatermarkColumnOnTarget) ? orderCol : job.WatermarkColumnOnTarget;
                var watermark = dstDb.useSQL().select("max(" + wmCol + ")").from(tableName).queryScalar<DateTime?>();

                DateTime? cursorTime = watermark;
                var cursorPk = string.Empty;
                var srcRepo = srcDb.useRepo<TSrc>();
                var dstRepo = dstDb.useRepo<TDst>();
                SyncJobHelpers.ApplyBulkOptions(dstRepo, host);

                long pulled = 0, inserted = 0, updated = 0, skipped = 0;

                while (true)
                {
                    if (!host.WaitIfPausedOrThrow())
                        break;

                    var ct = cursorTime;
                    var cpk = cursorPk;
                    List<TSrc> batch;
                    try
                    {
                        batch = srcRepo.GetList(kit =>
                        {
                            kit.whereIsNotNull(orderCol);
                            if (ct.HasValue)
                            {
                                kit.whereFormat(
                                    "((" + orderCol + " > {0}) or (" + orderCol + " = {0} and " + pkColSrc + " > {1}))",
                                    ct.Value, cpk ?? string.Empty);
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
                        Advance(batch, orderProp, pkPropSrc, ref cursorTime, ref cursorPk);
                        if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/map"))
                            return true;
                        SyncJobHelpers.DelayBatch(host);
                        continue;
                    }

                    try
                    {
                        var pkValues = new List<string>();
                        var byPk = new Dictionary<string, TDst>(StringComparer.OrdinalIgnoreCase);
                        foreach (var row in mapped)
                        {
                            var pk = pkPropDst.GetValue(row)?.ToString();
                            if (string.IsNullOrEmpty(pk))
                            {
                                skipped++;
                                continue;
                            }
                            pkValues.Add(pk);
                            byPk[pk] = row;
                        }

                        var existing = SyncJobHelpers.FindExistingPks(dstDb, tableName, pkColDst, pkPropDst, pkValues);
                        var toInsert = new List<TDst>();
                        var toUpdate = new List<TDst>();

                        foreach (var kv in byPk)
                        {
                            if (!existing.Contains(kv.Key))
                            {
                                toInsert.Add(kv.Value);
                                continue;
                            }

                            // 简化：存在则更新（Modified 精细比较可后续加强：先查目标行）
                            toUpdate.Add(kv.Value);
                        }

                        if (toInsert.Count > 0)
                            inserted += dstRepo.InsertRange(toInsert);
                        if (toUpdate.Count > 0)
                            updated += dstRepo.UpdateRange(toUpdate);
                    }
                    catch (Exception ex)
                    {
                        Advance(batch, orderProp, pkPropSrc, ref cursorTime, ref cursorPk);
                        if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/upsert"))
                            return true;
                        SyncJobHelpers.DelayBatch(host);
                        continue;
                    }

                    Advance(batch, orderProp, pkPropSrc, ref cursorTime, ref cursorPk);
                    if (batch.Count < batchSize)
                        break;
                    SyncJobHelpers.DelayBatch(host);
                }

                host.AppendProgress(
                    "OK " + typeName + "/" + tableName
                    + ": pulled=" + pulled + ", inserted=" + inserted
                    + ", updated=" + updated + ", skipped=" + skipped);
                return false;
            }
            catch (Exception ex)
            {
                return SyncJobHelpers.HandleBatchError(host, job, ex, typeName);
            }
        }

        static void Advance<TSrc>(IList<TSrc> batch, PropertyInfo orderProp, PropertyInfo pkProp,
            ref DateTime? cursorTime, ref string cursorPk)
        {
            if (batch == null || batch.Count == 0) return;
            var last = batch[batch.Count - 1];
            cursorTime = orderProp.GetValue(last) as DateTime?;
            cursorPk = pkProp.GetValue(last)?.ToString() ?? string.Empty;
        }
    }
}
