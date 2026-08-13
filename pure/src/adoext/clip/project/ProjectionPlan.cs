using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using mooSQL.data;

namespace mooSQL.data.clip.project
{
    internal sealed class ColumnSlot
    {
        public int Index { get; set; }
        public string Alias { get; set; }
        public ColumnRoot Root { get; set; }
    }

    /// <summary>
    /// Select 尾投影计划：槽位 SELECT + 编译后的行投影器。
    /// </summary>
    internal sealed class ProjectionPlan
    {
        public LambdaExpression Source { get; set; }
        public List<ColumnSlot> Slots { get; } = new List<ColumnSlot>();
        public Dictionary<string, ColumnSlot> SlotsByKey { get; } = new Dictionary<string, ColumnSlot>(StringComparer.Ordinal);
        public Delegate CompiledProjector { get; set; }

        public ColumnSlot GetOrAddSlot(ColumnRoot root)
        {
            if (SlotsByKey.TryGetValue(root.Key, out var existing))
                return existing;

            var slot = new ColumnSlot
            {
                Index = Slots.Count,
                Alias = "__c" + Slots.Count,
                Root = root,
            };
            Slots.Add(slot);
            SlotsByKey[root.Key] = slot;
            return slot;
        }

        public IEnumerable<T> ExecuteQuery<T>(SQLBuilder builder)
        {
            var projector = (Func<RowBag, T>)CompiledProjector;
            var slots = Slots;
            return builder.queryReader((DbDataReader reader) =>
            {
                var bag = RowBag.FromReader(reader, slots);
                return projector(bag);
            });
        }

        public T ExecuteUnique<T>(SQLBuilder builder)
        {
            var list = ExecuteQuery<T>(builder);
            T found = default;
            var n = 0;
            foreach (var item in list)
            {
                n++;
                if (n > 1)
                    return default;
                found = item;
            }
            return n == 1 ? found : default;
        }

        public PageOutput<T> ExecutePage<T>(SQLBuilder builder)
        {
            var projector = (Func<RowBag, T>)CompiledProjector;
            var paged = builder.queryPaged();
            var items = new List<T>();
            if (paged?.Items != null)
            {
                foreach (DataRow row in paged.Items.Rows)
                    items.Add(projector(RowBag.FromDataRow(row, Slots)));
            }
            return new PageOutput<T>
            {
                Items = items,
                Total = paged?.Total ?? 0,
                PageNum = paged?.PageNum ?? 0,
                PageSize = paged?.PageSize ?? 0,
            };
        }
    }

    internal sealed class RowBag
    {
        private readonly object[] _values;

        public RowBag(int size)
        {
            _values = new object[size];
        }

        public static RowBag FromDataRow(DataRow row, List<ColumnSlot> slots)
        {
            var bag = new RowBag(slots.Count);
            for (int i = 0; i < slots.Count; i++)
            {
                var v = row[slots[i].Alias];
                bag._values[i] = v == DBNull.Value ? null : v;
            }
            return bag;
        }

        /// <summary>
        /// 按 SELECT 槽位序读取（与 __c0.. 顺序一致）。
        /// </summary>
        public static RowBag FromReader(DbDataReader reader, List<ColumnSlot> slots)
        {
            var bag = new RowBag(slots.Count);
            var fieldCount = reader.FieldCount;
            for (int i = 0; i < slots.Count; i++)
            {
                if (i >= fieldCount)
                {
                    bag._values[i] = null;
                    continue;
                }
                var v = reader.GetValue(i);
                bag._values[i] = v == DBNull.Value ? null : v;
            }
            return bag;
        }

        public T Get<T>(int index)
        {
            var v = _values[index];
            if (v == null)
                return default;

            if (v is T t)
                return t;

            var target = typeof(T);
            var underlying = Nullable.GetUnderlyingType(target);
            if (underlying != null)
            {
                if (v.GetType() == underlying)
                    return (T)v;
                return (T)Convert.ChangeType(v, underlying);
            }

            return (T)Convert.ChangeType(v, target);
        }
    }
}
