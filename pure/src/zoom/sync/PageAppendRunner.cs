///////////////////////////////////////////////////////////
//  PageAppendRunner.cs
//  模式 2/3：翻页拉取 + Append（可探活）
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace mooSQL.data.sync
{
    internal static class PageAppendRunner
    {
        public static bool Run<TSrc, TDst>(SyncTableJob<TSrc, TDst> job, DataSyncHost host, bool probePk)
            where TSrc : class, new()
            where TDst : class, new()
        {
            var typeName = typeof(TSrc).Name;
            try
            {
                var dstType = typeof(TDst);
                var tableName = SyncEntityMeta.ResolveTableName(dstType);
                var (pkPropDst, pkColDst) = SyncEntityMeta.ResolvePrimaryKey(dstType);
                SyncEntityMeta.EnsureSafeIdent(tableName);
                SyncEntityMeta.EnsureSafeIdent(pkColDst);

                var srcPos = SyncJobHelpers.SourcePos(job, host);
                var dstPos = SyncJobHelpers.TargetPos(job, host);
                var srcDb = SyncJobHelpers.Db(host, srcPos);
                var dstDb = SyncJobHelpers.Db(host, dstPos);

                if (host.Options.EnsureTargetSchema)
                    dstDb.ensureSchema<TDst>(host.Options.SchemaMode);

                var pageSize = SyncJobHelpers.EffectiveBatchSize(job, host);
                var srcRepo = srcDb.useRepo<TSrc>();
                var dstRepo = dstDb.useRepo<TDst>();
                SyncJobHelpers.ApplyBulkOptions(dstRepo, host);

                var pull = job.ResolvePullMode();
                string? pkColSrc = null;
                if (pull == DataSyncPullMode.EntityByPk)
                {
                    var (pkPropSrc, pkCol) = SyncEntityMeta.ResolvePrimaryKey(typeof(TSrc));
                    pkColSrc = pkCol;
                    SyncEntityMeta.EnsureSafeIdent(pkColSrc);
                }

                long pulled = 0, inserted = 0, skipped = 0;
                var page = 1;

                while (true)
                {
                    if (!host.WaitIfPausedOrThrow())
                        break;

                    List<TSrc> batch;
                    var pageNum = page;
                    try
                    {
                        if (pull == DataSyncPullMode.FromSql)
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
                            batch = srcRepo.GetList(kit =>
                            {
                                job.OnSourceQuery?.Invoke(kit);
                                if (kit.current == null || kit.current.orderPart == null || kit.current.orderPart.Count == 0)
                                    kit.orderBy(pkColSrc + " asc");
                                kit.setPage(pageSize, pageNum);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        page++;
                        if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/page" + pageNum))
                            return true;
                        SyncJobHelpers.DelayBatch(host);
                        continue;
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
                        page++;
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
                        page++;
                        if (SyncJobHelpers.HandleBatchError(host, job, ex, typeName + "/write"))
                            return true;
                        SyncJobHelpers.DelayBatch(host);
                        continue;
                    }

                    if (batch.Count < pageSize)
                        break;

                    page++;
                    SyncJobHelpers.DelayBatch(host);
                }

                host.AppendProgress(
                    "OK " + typeName + "/" + tableName
                    + ": pulled=" + pulled + ", inserted=" + inserted
                    + ", skipped=" + skipped + ", pages~=" + page);
                return false;
            }
            catch (Exception ex)
            {
                return SyncJobHelpers.HandleBatchError(host, job, ex, typeName);
            }
        }

    }
}
