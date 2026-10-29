///////////////////////////////////////////////////////////
//  PageAppendRunner.cs
//  模式 2：offset 翻页；模式 3：主键键集翻页 + Append（可探活）
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Reflection;

namespace mooSQL.data.sync
{
    internal static class PageAppendRunner
    {
        public static bool Run<TSrc, TDst>(SyncTableJob<TSrc, TDst> job, SooDataSyncHost host, bool probePk)
            where TSrc : class, new()
            where TDst : class, new()
        {
            var typeName = typeof(TSrc).Name;
            try
            {
                var dstType = typeof(TDst);
                var tableName = SyncEntityMeta.ResolveTableName(dstType);
                var (pkPropDst, pkColDst) = SyncEntityMeta.ResolvePrimaryKey(dstType, job.KeyColumn);
                SyncEntityMeta.EnsureSafeIdent(tableName);
                SyncEntityMeta.EnsureSafeIdent(pkColDst);

                var srcDb = SyncJobHelpers.SourceDb(job, host);
                var dstDb = SyncJobHelpers.TargetDb(job, host);

                if (host.Options.EnsureTargetSchema)
                    dstDb.ensureSchema<TDst>(host.Options.SchemaMode);

                var pageSize = SyncJobHelpers.EffectiveBatchSize(job, host);
                var srcRepo = srcDb.useRepo<TSrc>();
                var dstRepo = dstDb.useRepo<TDst>();
                SyncJobHelpers.ApplyBulkOptions(dstRepo, host);

                var pull = job.ResolvePullMode();
                PropertyInfo? pkPropSrc = null;
                string? pkColSrc = null;
                if (pull == SooDataSyncPullMode.EntityByPk)
                {
                    (pkPropSrc, pkColSrc) = SyncEntityMeta.ResolvePrimaryKey(typeof(TSrc), job.KeyColumn);
                    SyncEntityMeta.EnsureSafeIdent(pkColSrc);
                }

                long pulled = 0, inserted = 0, skipped = 0;
                var page = 1;
                object? keyCursor = null;

                while (true)
                {
                    if (!host.WaitIfPausedOrThrow())
                        break;

                    List<TSrc> batch;
                    var pageNum = page;
                    var cursorSnap = keyCursor;
                    try
                    {
                        if (pull == SooDataSyncPullMode.FromSql)
                        {
                            if (job.FromSql == null)
                                throw new InvalidOperationException("模式 FromSql 需要 fromSql 配置");
                            var kit = srcDb.useSQL();
                            job.FromSql(kit);
                            kit.setPage(pageSize, pageNum);
                            batch = new List<TSrc>(kit.query<TSrc>());
                        }
                        else
                        {
                            // 模式 3：键集翻页（pk > cursor），避免大偏移
                            batch = srcRepo.GetList(kit =>
                            {
                                SyncJobHelpers.ApplyKeysetPage(
                                    kit, pkColSrc!, cursorSnap, pageSize, job.OnSourceQuery);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // FromSql 可页码+1；键集无批则无法安全推进，结束本表
                        if (pull == SooDataSyncPullMode.FromSql)
                        {
                            page++;
                            if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/page" + pageNum))
                                return true;
                            SyncJobHelpers.DelayBatch(host);
                            continue;
                        }

                        return SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/keyset");
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
                        AdvancePageOrKeyset(pull, batch, pkPropSrc, ref page, ref keyCursor);
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
                        AdvancePageOrKeyset(pull, batch, pkPropSrc, ref page, ref keyCursor);
                        if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/write"))
                            return true;
                        SyncJobHelpers.DelayBatch(host);
                        continue;
                    }

                    if (batch.Count < pageSize)
                        break;

                    AdvancePageOrKeyset(pull, batch, pkPropSrc, ref page, ref keyCursor);
                    SyncJobHelpers.DelayBatch(host);
                }

                host.AppendProgress(
                    "OK " + typeName + "/" + tableName
                    + ": pulled=" + pulled + ", inserted=" + inserted
                    + ", skipped=" + skipped
                    + (pull == SooDataSyncPullMode.EntityByPk ? ", keyset" : ", pages~=" + page));
                return false;
            }
            catch (Exception ex)
            {
                return SyncJobHelpers.HandleBatchError(host, job, ex, typeName);
            }
        }

        static void AdvancePageOrKeyset<TSrc>(
            SooDataSyncPullMode pull,
            IList<TSrc> batch,
            PropertyInfo? pkPropSrc,
            ref int page,
            ref object? keyCursor)
        {
            if (pull == SooDataSyncPullMode.FromSql)
            {
                page++;
                return;
            }
            if (batch == null || batch.Count == 0 || pkPropSrc == null)
                return;
            keyCursor = SyncJobHelpers.GetPkValue(pkPropSrc, batch[batch.Count - 1]!);
        }
    }
}
