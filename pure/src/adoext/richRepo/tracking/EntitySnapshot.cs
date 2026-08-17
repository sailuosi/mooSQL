using System;
using System.Collections.Generic;
using System.IO;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 实体原始值快照（对比用值相等，不用 GetHashCode）。
    /// </summary>
    public sealed class EntitySnapshot
    {
        readonly object _entity;
        readonly EntityInfo _meta;
        readonly TrackingOptions _options;
        readonly Dictionary<string, object> _origin;

        /// <summary>
        /// 对已映射列做原始值快照。
        /// </summary>
        public EntitySnapshot(object entity, EntityInfo meta, TrackingOptions options = null)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
            _meta = meta ?? throw new ArgumentNullException(nameof(meta));
            _options = options ?? new TrackingOptions();
            _origin = new Dictionary<string, object>(StringComparer.Ordinal);
            Capture();
        }

        /// <summary>关联实体。</summary>
        public object Entity => _entity;

        /// <summary>是否存在相对快照的变更。</summary>
        public bool HasChanges => GetDiff().Count > 0;

        /// <summary>与当前实体属性逐字段比较，返回需更新的列（不含 PK、不含累加前缀）。</summary>
        public Dictionary<string, object> GetDiff()
        {
            var diff = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var col in _meta.Columns)
            {
                if (!ShouldTrack(col)) continue;
                if (col.IsPrimarykey) continue;

                var current = col.PropertyInfo?.GetValue(_entity);
                _origin.TryGetValue(col.PropertyName, out var origin);
                if (!ValuesEqual(origin, current))
                    diff[col.PropertyName] = current;
            }
            return diff;
        }

        /// <summary>用当前值刷新快照。</summary>
        public void AcceptChanges() => Capture();

        void Capture()
        {
            _origin.Clear();
            foreach (var col in _meta.Columns)
            {
                if (!ShouldTrack(col)) continue;
                var val = col.PropertyInfo?.GetValue(_entity);
                _origin[col.PropertyName] = CloneScalar(val);
            }
        }

        bool ShouldTrack(EntityColumn col)
        {
            if (col == null) return false;
            if (col.IsIgnore || col.Navigat != null) return false;
            if (col.Kind != FieldKind.Base && col.Kind != FieldKind.None) return false;
            if (_options.ExcludeMembers.Contains(col.PropertyName)) return false;
            if (_options.ExcludeLargeTypes && IsLargeType(col.PropertyInfo?.PropertyType)) return false;
            return true;
        }

        static bool IsLargeType(Type t)
        {
            if (t == null) return false;
            if (t == typeof(byte[]) || t == typeof(Stream)) return true;
            if (typeof(Stream).IsAssignableFrom(t)) return true;
            return false;
        }

        static object CloneScalar(object val)
        {
            if (val is byte[] bytes)
            {
                var copy = new byte[bytes.Length];
                Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);
                return copy;
            }
            return val;
        }

        static bool ValuesEqual(object a, object b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a is byte[] ba && b is byte[] bb)
            {
                if (ba.Length != bb.Length) return false;
                for (int i = 0; i < ba.Length; i++)
                    if (ba[i] != bb[i]) return false;
                return true;
            }
            if (a is string sa && b is string sb)
                return string.Equals(sa, sb, StringComparison.Ordinal);
            return Equals(a, b);
        }
    }
}
