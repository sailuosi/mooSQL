///////////////////////////////////////////////////////////
//  UpsertByModifiedRunner.cs
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Reflection;

namespace mooSQL.data.sync
{
    /// <summary>按主键探活后 InsertRange / UpdateRange（源 Modified ≥ 目标才更新）。</summary>
    internal static class UpsertByModifiedRunner
    {
        public static bool Run<TSrc, TDst>(SyncTableJob<TSrc, TDst> job, DataSyncHost host)
            where TSrc : class, new()
            where TDst : class, new()
        {
            var typeName = typeof(TSrc).Name;
            try
            {
                var srcType = typeof(TSrc);
                var dstType = typeof(TDst);
                var tableName = SyncEntityMeta.ResolveTableName(dstType);
                var (pkPropSrc, pkColSrc) = SyncEntityMeta.ResolvePrimaryKey(srcType);
                var (pkPropDst, pkColDst) = typeof(TSrc) == typeof(TDst)
                    ? (pkPropSrc, pkColSrc)
                    : SyncEntityMeta.ResolvePrimaryKey(dstType);

                SyncEntityMeta.EnsureSafeIdent(pkColDst);
                SyncEntityMeta.EnsureSafeIdent(tableName);

                var pull = job.ResolvePullMode();
                PropertyInfo? orderProp = null;
                string? orderCol = null;
                if (pull == DataSyncPullMode.Watermark)
                {
                    if (string.IsNullOrWhiteSpace(job.OrderByColumn))
                        throw new InvalidOperationException("UpsertByModified 水位模式需要 orderBy");
                    (orderProp, orderCol) = SyncEntityMeta.ResolveOrderBy(srcType, job.OrderByColumn);
                    SyncEntityMeta.EnsureSafeIdent(orderCol);
                    SyncEntityMeta.EnsureSafeIdent(pkColSrc);
                }

                var modColName = !string.IsNullOrWhiteSpace(job.ModifiedColumn)
                    ? job.ModifiedColumn
                    : (!string.IsNullOrWhiteSpace(job.OrderByColumn) ? job.OrderByColumn : null);
                if (string.IsNullOrWhiteSpace(modColName))
                    throw new InvalidOperationException("UpsertByModified 需要 modified 或 orderBy 指定修改时间列");

                var (modPropDst, modCol) = SyncEntityMeta.ResolveOrderBy(dstType, modColName);
                SyncEntityMeta.EnsureSafeIdent(modCol);

                var srcDb = SyncJobHelpers.Db(host, SyncJobHelpers.SourcePos(job, host));
                var dstDb = SyncJobHelpers.Db(host, SyncJobHelpers.TargetPos(job, host));
                if (host.Options.EnsureTargetSchema)
                    dstDb.ensureSchema<TDst>(host.Options.SchemaMode);

                var batchSize = SyncJobHelpers.EffectiveBatchSize(job, host);
                var srcRepo = srcDb.useRepo<TSrc>();
                var dstRepo = dstDb.useRepo<TDst>();
                SyncJobHelpers.ApplyBulkOptions(dstRepo, host);

                DateTime? cursorTime = null;
                var cursorPk = string.Empty;
                if (pull == DataSyncPullMode.Watermark)
                {
                    var wmCol = string.IsNullOrWhiteSpace(job.WatermarkColumnOnTarget) ? orderCol! : job.WatermarkColumnOnTarget;
                    SyncEntityMeta.EnsureSafeIdent(wmCol);
                    cursorTime = dstDb.useSQL().select("max(" + wmCol + ")").from(tableName).queryScalar<DateTime?>();
                }

                long pulled = 0, inserted = 0, updated = 0, skipped = 0;
                var page = 1;

                while (true)
                {
                    if (!host.WaitIfPausedOrThrow())
                        break;

                    List<TSrc> batch;
                    try
                    {
                        batch = PullBatch(job, host, srcDb, srcRepo, pull, orderCol ?? string.Empty, pkColSrc,
                            cursorTime, cursorPk, batchSize, page);
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
                        AdvanceAfterBatch(pull, batch, orderProp, pkPropSrc, ref cursorTime, ref cursorPk, ref page);
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

                        var targetMods = SyncJobHelpers.LoadTargetModifiedByPk(
                            dstDb, tableName, pkColDst, modCol, pkPropDst, pkValues);

                        var toInsert = new List<TDst>();
                        var toUpdate = new List<TDst>();

                        foreach (var kv in byPk)
                        {
                            if (!targetMods.TryGetValue(kv.Key, out var dstMod))
                            {
                                toInsert.Add(kv.Value);
                                continue;
                            }

                            var srcMod = modPropDst.GetValue(kv.Value) as DateTime?;
                            if (!srcMod.HasValue)
                            {
                                skipped++;
                                continue;
                            }

                            // 源修改时间 ≥ 目标才更新（含相等，保证同秒重放可覆盖）
                            if (!dstMod.HasValue || srcMod.Value >= dstMod.Value)
                                toUpdate.Add(kv.Value);
                            else
                                skipped++;
                        }

                        if (toInsert.Count > 0)
                            inserted += dstRepo.InsertRange(toInsert);
                        if (toUpdate.Count > 0)
                            updated += dstRepo.UpdateRange(toUpdate);
                    }
                    catch (Exception ex)
                    {
                        AdvanceAfterBatch(pull, batch, orderProp, pkPropSrc, ref cursorTime, ref cursorPk, ref page);
                        if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/upsert"))
                            return true;
                        SyncJobHelpers.DelayBatch(host);
                        continue;
                    }

                    AdvanceAfterBatch(pull, batch, orderProp, pkPropSrc, ref cursorTime, ref cursorPk, ref page);
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

        static List<TSrc> PullBatch<TSrc, TDst>(
            SyncTableJob<TSrc, TDst> job,
            DataSyncHost host,
            DBInstance srcDb,
            SooRepository<TSrc> srcRepo,
            DataSyncPullMode pull,
            string orderCol,
            string pkColSrc,
            DateTime? cursorTime,
            string cursorPk,
            int batchSize,
            int page)
            where TSrc : class, new()
            where TDst : class, new()
        {
            if (pull == DataSyncPullMode.FromSql)
            {
                if (job.FromSql == null)
                    throw new InvalidOperationException("模式 FromSql 需要 fromSql 配置");
                var kit = srcDb.useSQL();
                job.FromSql(kit);
                kit.setPage(batchSize, page);
                return new List<TSrc>(kit.query<TSrc>());
            }

            if (pull == DataSyncPullMode.EntityByPk)
            {
                return srcRepo.GetList(kit =>
                {
                    job.OnSourceQuery?.Invoke(kit);
                    if (kit.current == null || kit.current.orderPart == null || kit.current.orderPart.Count == 0)
                        kit.orderBy(pkColSrc + " asc");
                    kit.setPage(batchSize, page);
                });
            }

            // Watermark
            var ct = cursorTime;
            var cpk = cursorPk;
            return srcRepo.GetList(kit =>
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

        static void AdvanceAfterBatch<TSrc>(
            DataSyncPullMode pull,
            IList<TSrc> batch,
            PropertyInfo? orderProp,
            PropertyInfo pkProp,
            ref DateTime? cursorTime,
            ref string cursorPk,
            ref int page)
        {
            if (pull == DataSyncPullMode.Watermark)
            {
                if (batch == null || batch.Count == 0 || orderProp == null || pkProp == null)
                    return;
                var last = batch[batch.Count - 1];
                cursorTime = orderProp.GetValue(last) as DateTime?;
                cursorPk = pkProp.GetValue(last)?.ToString() ?? string.Empty;
            }
            else
            {
                page++;
            }
        }
    }
}
