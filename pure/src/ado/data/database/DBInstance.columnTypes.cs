using System;
using System.Collections.Concurrent;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 列物理类型按 Tag 的运行时登记（与 <see cref="DBTableCreator.columnTypeByTag"/> DDL 同源）。
    /// 供 string CLR + Guid/uuid 物理列在 where/set 时按 <see cref="DataFam.Guid"/> 绑参。
    /// </summary>
    public partial class DBInstance
    {
        readonly ConcurrentDictionary<string, (DataFam Type, int Length)> _tagColumnTypes =
            new ConcurrentDictionary<string, (DataFam, int)>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 登记 Tag → 物理 DataFam（后写覆盖）。由 <see cref="DBTableCreator.columnTypeByTag"/> 同步调用。
        /// </summary>
        public void RegisterColumnTypeByTag(string tag, DataFam dataType, int length = 0)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("tag must not be empty.", nameof(tag));
            _tagColumnTypes[tag.Trim()] = (dataType, length);
        }

        /// <summary>
        /// 查询已登记的 Tag 物理类型。
        /// </summary>
        public bool TryGetColumnTypeByTag(string tag, out DataFam dataType, out int length)
        {
            dataType = DataFam.Undefined;
            length = 0;
            if (string.IsNullOrWhiteSpace(tag))
                return false;
            if (!_tagColumnTypes.TryGetValue(tag.Trim(), out var cfg))
                return false;
            dataType = cfg.Type;
            length = cfg.Length;
            return true;
        }

        /// <summary>
        /// 列有效物理类型：属性 <see cref="EntityColumn.DataType"/> &gt; Tag 登记 &gt; CLR Guid。
        /// </summary>
        public DataFam GetEffectiveDataFam(EntityColumn col)
        {
            if (col == null)
                return DataFam.Undefined;

            if (col.DataType != DataFam.Undefined)
                return col.DataType;

            if (!string.IsNullOrWhiteSpace(col.Tag)
                && TryGetColumnTypeByTag(col.Tag, out var byTag, out _))
                return byTag;

            var under = col.UnderType;
            if (under == typeof(Guid) || under == typeof(Guid?))
                return DataFam.Guid;

            return DataFam.Undefined;
        }
    }
}
