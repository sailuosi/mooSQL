using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 仓储批量写入内核：多行 INSERT（复用 SQLBuilder 切片）、Bulk、Update 多语句分片。
    /// </summary>
    public static class EntityBatchWriter
    {
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

        static void ApplyInsertBatchSize(SQLBuilder kit, BatchWriteOptions options)
        {
            if (options == null || options.InsertBatchSize <= 0)
                return;
            if (kit.Inner is StepBuilder sb)
                sb.InsertValuesBatchSize = options.InsertBatchSize;
        }
    }
}
