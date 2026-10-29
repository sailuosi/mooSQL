///////////////////////////////////////////////////////////
//  SyncJobHelpers.cs
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace mooSQL.data.sync
{
    internal static class SyncJobHelpers
    {
        public static int EffectiveBatchSize(SyncTableJob job, DataSyncHost host)
        {
            var n = job.BatchSize ?? host.Options.BatchSize;
            return n <= 0 ? 2000 : n;
        }

        public static int SourcePos(SyncTableJob job, DataSyncHost host)
            => job.SourcePosition ?? host.Options.SourcePosition;

        public static int TargetPos(SyncTableJob job, DataSyncHost host)
            => job.TargetPosition ?? host.Options.TargetPosition;

        public static DataSyncFailureAction Failure(SyncTableJob job, DataSyncHost host)
            => job.FailureAction ?? host.Options.FailureAction;

        public static DBInstance Db(DataSyncHost host, int position)
            => host.ResolveDb(position);

        public static void ApplyBulkOptions<T>(SooRepository<T> repo, DataSyncHost host) where T : class, new()
        {
            if (repo == null || !host.Options.UseBulk)
                return;
            if (repo.BatchOptions == null)
                repo.BatchOptions = new BatchWriteOptions();
            repo.BatchOptions.Mode = BatchWriteMode.Auto;
        }

        public static List<TDst> MapRows<TSrc, TDst>(
            SyncTableJob<TSrc, TDst> job,
            IList<TSrc> batch) where TSrc : class, new() where TDst : class, new()
        {
            if (batch == null || batch.Count == 0)
                return new List<TDst>();

            List<TDst> mapped;
            if (job.MapBatch != null)
            {
                var list = job.MapBatch(batch as IReadOnlyList<TSrc> ?? batch.ToList());
                mapped = list == null ? new List<TDst>() : list.ToList();
            }
            else
            {
                mapped = new List<TDst>(batch.Count);
                var map = job.Map;
                for (var i = 0; i < batch.Count; i++)
                {
                    var src = batch[i];
                    TDst dst;
                    if (map != null)
                        dst = map(src);
                    else if (typeof(TSrc) == typeof(TDst))
                        dst = (TDst)(object)src;
                    else
                        throw new InvalidOperationException(
                            "异构同步 " + typeof(TSrc).Name + " → " + typeof(TDst).Name + " 必须提供 map");

                    if (dst == null)
                        continue;
                    mapped.Add(dst);
                }
            }

            if (job.OnAssignBatch != null)
                job.OnAssignBatch(mapped);

            if (job.OnAssign != null || job.OnAssignWithSource != null)
            {
                for (var i = 0; i < mapped.Count; i++)
                {
                    var dst = mapped[i];
                    job.OnAssign?.Invoke(dst);
                    if (job.OnAssignWithSource != null && i < batch.Count)
                        job.OnAssignWithSource(dst, batch[i]);
                }
            }

            return mapped;
        }

        public static HashSet<string> FindExistingPks(
            DBInstance targetDb,
            string tableName,
            string pkCol,
            PropertyInfo pkProp,
            List<string> pkValues)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (pkValues == null || pkValues.Count == 0)
                return set;

            const int chunkSize = 500;
            var isGuid = pkProp.PropertyType == typeof(Guid) || pkProp.PropertyType == typeof(Guid?);

            for (var i = 0; i < pkValues.Count; i += chunkSize)
            {
                var chunk = pkValues.Skip(i).Take(chunkSize).ToList();
                var kit = targetDb.useSQL().select(pkCol).from(tableName);
                if (isGuid)
                {
                    var guids = new List<Guid>();
                    foreach (var s in chunk)
                    {
                        if (Guid.TryParse(s, out var g))
                            guids.Add(g);
                    }
                    if (guids.Count == 0)
                        continue;
                    kit.whereInGuid(pkCol, guids);
                }
                else
                {
                    kit.whereIn(pkCol, chunk);
                }

                var dt = kit.query();
                if (dt == null || dt.Rows.Count == 0)
                    continue;
                foreach (DataRow row in dt.Rows)
                {
                    var v = row[0]?.ToString();
                    if (!string.IsNullOrEmpty(v))
                        set.Add(v);
                }
            }

            return set;
        }

        public static void DelayBatch(DataSyncHost host)
        {
            var gap = host.Options.BatchInterval;
            if (gap > TimeSpan.Zero)
                System.Threading.Thread.Sleep(gap);
        }

        public static bool HandleBatchError(DataSyncHost host, SyncTableJob job, Exception ex, string context)
        {
            host.AppendProgress("ERR " + context + ": " + ex.Message);
            return Failure(job, host) == DataSyncFailureAction.Abort;
        }
    }
}
