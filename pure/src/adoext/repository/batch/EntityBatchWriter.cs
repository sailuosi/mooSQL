using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// SaveRange S1 分流结果：空主键 / 库中不存在 → Insert；库中已存在 → Update。
    /// </summary>
    public sealed class SaveSplitResult<T>
    {
        /// <summary>走插入内核的实体。</summary>
        public List<T> ToInsert { get; } = new List<T>();
        /// <summary>走更新内核的实体。</summary>
        public List<T> ToUpdate { get; } = new List<T>();
    }

    /// <summary>
    /// 仓储批量写入内核：多行 INSERT（复用 SQLBuilder 切片）、Bulk、Update 多语句分片、Save 探活分流。
    /// </summary>
    public static class EntityBatchWriter
    {
        /// <summary>
        /// 按「主键是否为 null / 库中是否已存在」分流（对齐 <c>SaveInner</c>：任一侧 PK 为 null → 插入；否则批量探活）。
        /// </summary>
        public static SaveSplitResult<T> SplitSaveByExistence<T>(
            SQLBuilder kit,
            EntityTranslator translator,
            EntityInfo en,
            IList<T> entities,
            Func<string> tableNameLoader,
            int inChunkSize)
        {
            var result = new SaveSplitResult<T>();
            if (entities == null || entities.Count == 0)
                return result;
            if (en == null) throw new ArgumentNullException(nameof(en));
            if (translator == null) throw new ArgumentNullException(nameof(translator));

            var pks = en.GetPK();
            if (pks == null || pks.Count == 0)
                throw new Exception("无主键定义时，无法自动保存！");

            var candidates = new List<T>();
            foreach (var entity in entities)
            {
                if (entity == null) continue;
                if (HasAnyNullPrimaryKey(en, entity, kit))
                {
                    if (en.Insertable != false)
                        result.ToInsert.Add(entity);
                    continue;
                }
                candidates.Add(entity);
            }

            if (candidates.Count == 0)
                return result;

            var existing = LoadExistingPkKeySet(kit, translator, en, candidates, tableNameLoader, inChunkSize);
            foreach (var entity in candidates)
            {
                var key = FormatEntityPkKey(en, entity, kit);
                if (existing.Contains(key))
                {
                    if (en.Updatable != false)
                        result.ToUpdate.Add(entity);
                }
                else if (en.Insertable != false)
                {
                    result.ToInsert.Add(entity);
                }
            }
            return result;
        }

        /// <summary>
        /// 批量查询已存在主键键集合（字符串规范化，兼容单列/联合主键）。
        /// </summary>
        public static HashSet<string> LoadExistingPkKeySet<T>(
            SQLBuilder kit,
            EntityTranslator translator,
            EntityInfo en,
            IList<T> entitiesWithPk,
            Func<string> tableNameLoader,
            int inChunkSize)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (entitiesWithPk == null || entitiesWithPk.Count == 0)
                return set;
            if (kit == null) throw new ArgumentNullException(nameof(kit));

            var pks = en.GetPK();
            if (pks == null || pks.Count == 0)
                return set;

            var chunk = inChunkSize > 0 ? inChunkSize : 500;
            var maxPara = kit.Dialect != null && kit.Dialect.paramMaxSize > 0
                ? kit.Dialect.paramMaxSize
                : 2500;
            if (chunk > maxPara - 8)
                chunk = Math.Max(1, maxPara - 8);

            var table = ResolveTableName(translator, en, entitiesWithPk[0], tableNameLoader);
            var selectCols = BuildPkSelectList(pks);

            for (var i = 0; i < entitiesWithPk.Count; i += chunk)
            {
                var take = Math.Min(chunk, entitiesWithPk.Count - i);
                var slice = new List<T>(take);
                for (var j = 0; j < take; j++)
                    slice.Add(entitiesWithPk[i + j]);

                kit.clear();
                kit.from(table);
                kit.select(selectCols);
                translator.setPKWhereRange(kit, slice, en);
                var dt = kit.query();
                if (dt == null || dt.Rows.Count == 0)
                    continue;
                foreach (DataRow row in dt.Rows)
                {
                    var key = FormatRowPkKey(pks, row);
                    if (!string.IsNullOrEmpty(key))
                        set.Add(key);
                }
            }
            return set;
        }

        static bool HasAnyNullPrimaryKey(EntityInfo en, object entity, SQLBuilder kit)
        {
            var pks = en.GetPK();
            var exp = kit?.Dialect?.expression;
            foreach (var pk in pks)
            {
                var raw = pk.PropertyInfo.GetValue(entity);
                var val = exp != null ? exp.ResolveColumnValue(raw, pk) : raw;
                if (val == null)
                    return true;
            }
            return false;
        }

        static string FormatEntityPkKey(EntityInfo en, object entity, SQLBuilder kit)
        {
            var pks = en.GetPK();
            var exp = kit?.Dialect?.expression;
            var sb = new StringBuilder();
            for (var i = 0; i < pks.Count; i++)
            {
                if (i > 0) sb.Append('\u001f');
                var raw = pks[i].PropertyInfo.GetValue(entity);
                var val = exp != null ? exp.ResolveColumnValue(raw, pks[i]) : raw;
                sb.Append(NormalizePkPart(val));
            }
            return sb.ToString();
        }

        static string FormatRowPkKey(IList<EntityColumn> pks, DataRow row)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < pks.Count; i++)
            {
                if (i > 0) sb.Append('\u001f');
                var name = pks[i].DbColumnName;
                object val = null;
                if (row.Table.Columns.Contains(name))
                    val = row[name];
                else if (i < row.Table.Columns.Count)
                    val = row[i];
                if (val == DBNull.Value)
                    val = null;
                sb.Append(NormalizePkPart(val));
            }
            return sb.ToString();
        }

        static string NormalizePkPart(object val)
        {
            if (val == null) return string.Empty;
            if (val is IFormattable f)
                return f.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
            return Convert.ToString(val, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        static string BuildPkSelectList(IList<EntityColumn> pks)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < pks.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(pks[i].DbColumnName);
            }
            return sb.ToString();
        }

        static string ResolveTableName(EntityTranslator translator, EntityInfo en, object sample, Func<string> tableNameLoader)
        {
            if (tableNameLoader != null)
            {
                var n = tableNameLoader();
                if (!string.IsNullOrWhiteSpace(n))
                    return n;
            }
            return translator.GetResolvedTableName(en, sample, tableNameLoader);
        }

        /// <summary>
        /// 多行 VALUES 插入：逐实体钩子 + prepareInsert + newRow，一次 doInsert（内部可多 SQLSeg）。
        /// </summary>
        public static int InsertMultiValues<T>(
            SQLBuilder kit,
            EntityTranslator translator,
            IList<T> entities,
            Func<string> tableNameLoader,
            Action<T> beforeSave,
            Action<T, int> afterSave,
            BatchWriteOptions options) where T : class
        {
            if (entities == null || entities.Count == 0)
                return 0;
            if (kit == null) throw new ArgumentNullException(nameof(kit));
            if (translator == null) throw new ArgumentNullException(nameof(translator));

            ApplyInsertBatchSize(kit, options);

            kit.clear();
            var first = true;
            foreach (var entity in entities)
            {
                if (entity == null)
                    continue;
                beforeSave?.Invoke(entity);
                if (!first)
                    kit.newRow();
                first = false;
                var res = translator.prepareInsert(kit, entity, typeof(T), null, tableNameLoader);
                if (!res.Status)
                {
                    kit.Client?.Loggor?.LogError(res.Message);
                    throw new InvalidOperationException(res.Message ?? "prepareInsert failed.");
                }
            }

            if (first)
                return 0;

            var cc = kit.doInsert();
            if (afterSave != null)
            {
                foreach (var entity in entities)
                {
                    if (entity == null) continue;
                    afterSave(entity, cc > 0 ? 1 : 0);
                }
            }
            return cc;
        }

        /// <summary>
        /// Bulk 插入：Repo 钩子在调用前后；Bulk.addList 经 Translator 对齐列语义。
        /// </summary>
        public static int InsertBulk<T>(
            DBInstance db,
            DBExecutor executor,
            EntityTranslator translator,
            IList<T> entities,
            string tableName,
            Action<T> beforeSave,
            Action<T, int> afterSave,
            BatchWriteOptions options) where T : class
        {
            if (entities == null || entities.Count == 0)
                return 0;
            if (db == null) throw new ArgumentNullException(nameof(db));

            foreach (var entity in entities)
            {
                if (entity != null)
                    beforeSave?.Invoke(entity);
            }

            var bk = db.useBulk();
            if (executor != null)
                bk.useTransaction(executor);
            if (!string.IsNullOrWhiteSpace(tableName))
                bk.setTable(tableName);
            if (options != null && options.InsertBatchSize > 0)
                bk.batchSize = options.InsertBatchSize;

            bk.addListAligned(translator, entities, tableName);
            var cc = bk.doInsert();

            if (afterSave != null)
            {
                foreach (var entity in entities)
                {
                    if (entity == null) continue;
                    afterSave(entity, cc > 0 ? 1 : 0);
                }
            }
            return cc;
        }

        /// <summary>
        /// 更新：分片 prepareUpdate + toUpdate 入队，每片一次 exeNonQuery。
        /// </summary>
        public static int UpdateMultiStatement<T>(
            SQLBuilder kit,
            EntityTranslator translator,
            IList<T> entities,
            Func<string> tableNameLoader,
            Action<T> beforeSave,
            Action<T, int> afterSave,
            BatchWriteOptions options) where T : class
        {
            if (entities == null || entities.Count == 0)
                return 0;

            var batchSize = options != null && options.UpdateBatchSize > 0
                ? options.UpdateBatchSize
                : 100;
            var total = 0;
            var cmds = new List<SQLCmd>(batchSize);
            var batchEntities = new List<T>(batchSize);

            void Flush()
            {
                if (cmds.Count == 0) return;
                var cc = kit.exeNonQuery(cmds);
                total += cc;
                if (afterSave != null)
                {
                    foreach (var entity in batchEntities)
                        afterSave(entity, cc > 0 ? 1 : 0);
                }
                cmds.Clear();
                batchEntities.Clear();
            }

            foreach (var entity in entities)
            {
                if (entity == null) continue;
                beforeSave?.Invoke(entity);
                kit.clear();
                var res = translator.prepareUpdate(kit, entity, typeof(T), null, tableNameLoader);
                if (!res.Status)
                {
                    kit.Client?.Loggor?.LogError(res.Message);
                    throw new InvalidOperationException(res.Message ?? "prepareUpdate failed.");
                }
                var cmd = kit.toUpdate();
                if (cmd != null && !cmd.Empty)
                {
                    cmds.Add(cmd);
                    batchEntities.Add(entity);
                }
                if (cmds.Count >= batchSize)
                    Flush();
            }
            Flush();
            return total;
        }

        /// <summary>
        /// 仅构建 UpdateUnion 命令（不执行）。供 Repo 执行与 UoW 入队共用。
        /// 不支持 / 无有效行 / 构建失败返回 null。
        /// </summary>
        public static SQLCmd TryBuildUpdateUnionCmd<T>(
            SQLBuilder kit,
            EntityTranslator translator,
            EntityInfo entityInfo,
            IList<T> entities,
            Func<string> tableNameLoader,
            Action<T> beforeSave,
            BatchWriteOptions options,
            out List<T> filled)
        {
            filled = new List<T>();
            if (entities == null || entities.Count == 0)
                return null;
            if (kit == null) throw new ArgumentNullException(nameof(kit));
            if (translator == null) throw new ArgumentNullException(nameof(translator));
            if (kit.Dialect == null || !kit.Dialect.SupportsUpdateUnion())
                return null;

            var en = entityInfo;
            if (en == null)
                en = kit.DBLive?.client?.EntityCash?.getEntityInfo(typeof(T));
            if (en == null)
                return null;

            var keyColumns = GetPrimaryKeyColumnNames(en);
            if (keyColumns.Count == 0)
                return null;

            ApplyUpdateUnionBatchSize(kit, options);

            kit.clear();
            var first = true;
            foreach (var entity in entities)
            {
                if (entity == null)
                    continue;
                beforeSave?.Invoke(entity);
                if (!first)
                    kit.newRow();
                first = false;
                var res = translator.prepareUpdateUnion(kit, entity, typeof(T), en, tableNameLoader);
                if (!res.Status)
                {
                    kit.Client?.Loggor?.LogError(res.Message);
                    throw new InvalidOperationException(res.Message ?? "prepareUpdateUnion failed.");
                }
                filled.Add(entity);
            }

            if (first)
                return null;

            var cmd = kit.toUpdateUnion(keyColumns);
            if (cmd == null || cmd.Empty)
                return null;
            return cmd;
        }

        /// <summary>
        /// UpdateUnion（U2）：钩子后填行并执行。方言不支持或构建失败时返回 -1（调用方降级 U1）。
        /// </summary>
        public static int UpdateUnion<T>(
            SQLBuilder kit,
            EntityTranslator translator,
            EntityInfo entityInfo,
            IList<T> entities,
            Func<string> tableNameLoader,
            Action<T> beforeSave,
            Action<T, int> afterSave,
            BatchWriteOptions options)
        {
            if (entities == null || entities.Count == 0)
                return 0;
            if (kit == null) throw new ArgumentNullException(nameof(kit));
            if (kit.Dialect == null || !kit.Dialect.SupportsUpdateUnion())
                return -1;

            var cmd = TryBuildUpdateUnionCmd(
                kit, translator, entityInfo, entities, tableNameLoader, beforeSave, options, out var filled);
            if (filled.Count == 0)
                return 0;
            if (cmd == null)
                return -1;

            var cc = kit.exeNonQuery(cmd);
            kit.clear();

            if (afterSave != null)
            {
                foreach (var entity in filled)
                    afterSave(entity, cc > 0 ? 1 : 0);
            }
            return cc;
        }

        /// <summary>实体主键物理列名（有序）。</summary>
        public static List<string> GetPrimaryKeyColumnNames(EntityInfo en)
        {
            var list = new List<string>();
            if (en == null) return list;
            var pks = en.GetPK();
            if (pks == null) return list;
            for (var i = 0; i < pks.Count; i++)
            {
                var col = pks[i];
                if (col == null || string.IsNullOrEmpty(col.DbColumnName))
                    continue;
                list.Add(col.DbColumnName);
            }
            return list;
        }

        static void ApplyInsertBatchSize(SQLBuilder kit, BatchWriteOptions options)
        {
            if (options == null || options.InsertBatchSize <= 0)
                return;
            if (kit.Inner is StepBuilder sb)
                sb.InsertValuesBatchSize = options.InsertBatchSize;
        }

        static void ApplyUpdateUnionBatchSize(SQLBuilder kit, BatchWriteOptions options)
        {
            if (options == null || options.UpdateUnionBatchSize <= 0)
                return;
            if (kit.Inner is StepBuilder sb)
                sb.UpdateUnionBatchSize = options.UpdateUnionBatchSize;
        }
    }
}
