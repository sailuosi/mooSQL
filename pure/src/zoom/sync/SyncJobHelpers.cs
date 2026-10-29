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
            // 与源一一配对，避免 map 跳过 null 后 onAssign(dst,src) 索引错位
            List<TSrc>? pairedSrc = null;

            if (job.MapBatch != null)
            {
                var list = job.MapBatch(batch as IReadOnlyList<TSrc> ?? batch.ToList());
                mapped = list == null ? new List<TDst>() : list.ToList();
                // mapBatch 仅当条数一致时才允许带源 assign
                if (job.OnAssignWithSource != null && mapped.Count == batch.Count)
                    pairedSrc = batch as List<TSrc> ?? batch.ToList();
            }
            else
            {
                mapped = new List<TDst>(batch.Count);
                if (job.OnAssignWithSource != null)
                    pairedSrc = new List<TSrc>(batch.Count);
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
                    pairedSrc?.Add(src);
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
                    if (job.OnAssignWithSource != null && pairedSrc != null && i < pairedSrc.Count)
                        job.OnAssignWithSource(dst, pairedSrc[i]);
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

            foreach (var row in QueryPkPairs(targetDb, tableName, pkCol, null, pkProp, pkValues))
            {
                if (!string.IsNullOrEmpty(row.Pk))
                    set.Add(row.Pk);
            }
            return set;
        }

        /// <summary>
        /// 按主键批查目标：PK + 可选修改时间列。
        /// </summary>
        public static Dictionary<string, DateTime?> LoadTargetModifiedByPk(
            DBInstance targetDb,
            string tableName,
            string pkCol,
            string modifiedCol,
            PropertyInfo pkProp,
            List<string> pkValues)
        {
            var map = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
            if (pkValues == null || pkValues.Count == 0)
                return map;

            foreach (var row in QueryPkPairs(targetDb, tableName, pkCol, modifiedCol, pkProp, pkValues))
            {
                if (string.IsNullOrEmpty(row.Pk))
                    continue;
                map[row.Pk] = row.Modified;
            }
            return map;
        }

        static List<(string Pk, DateTime? Modified)> QueryPkPairs(
            DBInstance targetDb,
            string tableName,
            string pkCol,
            string? modifiedCol,
            PropertyInfo pkProp,
            List<string> pkValues)
        {
            var list = new List<(string Pk, DateTime? Modified)>();
            const int chunkSize = 500;
            var isGuid = pkProp.PropertyType == typeof(Guid) || pkProp.PropertyType == typeof(Guid?);
            var selectCols = string.IsNullOrWhiteSpace(modifiedCol)
                ? pkCol
                : pkCol + "," + modifiedCol;

            for (var i = 0; i < pkValues.Count; i += chunkSize)
            {
                var chunk = pkValues.Skip(i).Take(chunkSize).ToList();
                var kit = targetDb.useSQL().select(selectCols).from(tableName);
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
                    var pk = row[0]?.ToString();
                    if (string.IsNullOrEmpty(pk))
                        continue;
                    DateTime? mod = null;
                    if (!string.IsNullOrWhiteSpace(modifiedCol) && dt.Columns.Count > 1 && row[1] != null && row[1] != DBNull.Value)
                    {
                        if (row[1] is DateTime dtv)
                            mod = dtv;
                        else if (DateTime.TryParse(row[1].ToString(), out var parsed))
                            mod = parsed;
                    }
                    list.Add((pk, mod));
                }
            }

            return list;
        }

        public static void DelayBatch(DataSyncHost host)
        {
            var gap = host.Options.BatchInterval;
            if (gap <= TimeSpan.Zero)
                return;
            // 切片等待，便于 Pause/Stop/Abort 打断
            var until = DateTime.UtcNow + gap;
            while (DateTime.UtcNow < until)
            {
                if (!host.WaitIfPausedOrThrow())
                    return;
                var left = until - DateTime.UtcNow;
                if (left <= TimeSpan.Zero)
                    break;
                var slice = left.TotalMilliseconds > 50 ? 50 : (int)Math.Max(1, left.TotalMilliseconds);
                System.Threading.Thread.Sleep(slice);
            }
        }

        public static bool HandleBatchError(DataSyncHost host, SyncTableJob job, Exception ex, string context)
        {
            host.AppendProgress("ERR " + context + ": " + ex.Message);
            return Failure(job, host) == DataSyncFailureAction.Abort;
        }

        /// <summary>
        /// 键集翻页条件：orderBy pk，可选 pk &gt; afterValue。
        /// </summary>
        public static void ApplyKeysetPage(
            SQLBuilder kit,
            string pkCol,
            object? afterPkValue,
            int pageSize,
            Action<SQLBuilder>? extraWhere)
        {
            extraWhere?.Invoke(kit);
            kit.whereIsNotNull(pkCol);
            if (afterPkValue != null && !(afterPkValue is DBNull))
                kit.whereFormat("(" + pkCol + " > {0})", afterPkValue);
            if (kit.current == null || kit.current.orderPart == null || kit.current.orderPart.Count == 0)
                kit.orderBy(pkCol + " asc");
            kit.top(pageSize);
        }

        /// <summary>从实体取主键值（typed，供键集游标）。</summary>
        public static object? GetPkValue(PropertyInfo pkProp, object row)
        {
            if (row == null || pkProp == null)
                return null;
            return pkProp.GetValue(row);
        }

        public static string? PkToString(object? pk)
        {
            return pk?.ToString();
        }

        /// <summary>批量按主键删除目标行。</summary>
        public static int DeleteByPkValues(
            DBInstance targetDb,
            string tableName,
            string pkCol,
            PropertyInfo pkProp,
            List<string> pkValues)
        {
            if (pkValues == null || pkValues.Count == 0)
                return 0;

            var deleted = 0;
            const int chunkSize = 500;
            var isGuid = pkProp.PropertyType == typeof(Guid) || pkProp.PropertyType == typeof(Guid?);

            for (var i = 0; i < pkValues.Count; i += chunkSize)
            {
                var chunk = pkValues.Skip(i).Take(chunkSize).ToList();
                var kit = targetDb.useSQL().from(tableName);
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
                var n = kit.doDelete();
                if (n > 0)
                    deleted += n;
            }

            return deleted;
        }
    }
}
