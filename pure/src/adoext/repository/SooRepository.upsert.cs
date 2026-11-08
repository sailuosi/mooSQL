using System;
using System.Collections.Generic;
using mooSQL.data.model;

namespace mooSQL.data
{
    public partial class SooRepository<T>
    {
        /// <summary>
        /// 方言 upsert（与 SQLBuilder.doUpsert / UpsertMode 同族）。
        /// 匹配键固定为实体主键；双写列 set、主键 setI、仅更新列 setU。
        /// 与 <see cref="Save"/>（PK 探活 + Insert/Update）不同。
        /// </summary>
        public int Upsert(T entity, UpsertMode mode = UpsertMode.Auto)
        {
            if (entity == null)
                return 0;

            var pks = En.GetPK();
            if (pks == null || pks.Count == 0)
                throw new InvalidOperationException("Upsert 需要实体主键定义。");

            var kit = getKit();
            OnBeforeSave(entity);

            var table = Translator.GetResolvedTableName(En, entity, tryTableNameLoader(tbname));
            kit.setTable(table);

            var exp = kit.Dialect.expression;
            foreach (var col in En.Columns)
            {
                if (col == null || col.IsIgnore || col.PropertyInfo == null)
                    continue;
                if ((col.Kind == FieldKind.Base || col.Kind == FieldKind.None) == false)
                    continue;

                var val = exp.ResolveColumnValue(col.PropertyInfo.GetValue(entity), col);
                var insertable = !col.IsOnlyIgnoreInsert;
                var updatable = !col.IsPrimarykey && !col.IsOnlyIgnoreUpdate;
                // 同一列勿先后 setI+setU（后者覆盖标志）；双写用 set
                if (insertable && updatable)
                    kit.set(col.DbColumnName, val);
                else if (insertable)
                    kit.setI(col.DbColumnName, val);
                else if (updatable)
                    kit.setU(col.DbColumnName, val);
            }

            Translator.setPKWhere(kit, entity, En);

            var cc = kit.doUpsert(mode);
            OnAfterSave(entity, cc);
            return cc;
        }

        /// <summary>
        /// 批量 upsert（逐条调用 <see cref="Upsert"/>，累加影响行数）。
        /// </summary>
        public int UpsertRange(IEnumerable<T> entities, UpsertMode mode = UpsertMode.Auto)
        {
            if (entities == null)
                return 0;

            var total = 0;
            foreach (var entity in entities)
            {
                if (entity == null)
                    continue;
                var n = Upsert(entity, mode);
                if (n > 0)
                    total += n;
            }
            return total;
        }
    }
}
