using System;
using System.Collections.Generic;

namespace mooSQL.data.richRepo.tracking
{
    /// <summary>
    /// 实体脏字段袋（对标 CRL IModel.Changes）。键以 '$' 前缀表示累加表达式更新。
    /// </summary>
    public sealed class EntityChangeBag
    {
        public const char CumulationPrefix = '$';

        readonly Dictionary<string, object> _changes = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>标记成员为脏并记录目标值。</summary>
        public void Set(string memberName, object value)
        {
            if (string.IsNullOrEmpty(memberName)) return;
            _changes[memberName] = value;
        }

        /// <summary>累加更新：SET col = col + @delta。</summary>
        public void SetCumulation(string memberName, object delta)
        {
            if (string.IsNullOrEmpty(memberName)) return;
            _changes[CumulationPrefix + memberName] = delta;
        }

        /// <summary>是否有脏字段。</summary>
        public bool IsModified => _changes.Count > 0;

        /// <summary>当前脏字段快照。</summary>
        public IReadOnlyDictionary<string, object> GetChanges()
        {
            return new Dictionary<string, object>(_changes, StringComparer.Ordinal);
        }

        /// <summary>清空。</summary>
        public void Clear() => _changes.Clear();

        /// <summary>解析键是否为累加，并得到真实属性名。</summary>
        public static bool TryParseMember(string key, out string memberName, out bool isCumulation)
        {
            memberName = null;
            isCumulation = false;
            if (string.IsNullOrEmpty(key)) return false;
            if (key[0] == CumulationPrefix)
            {
                isCumulation = true;
                memberName = key.Substring(1);
                return memberName.Length > 0;
            }
            memberName = key;
            return true;
        }
    }
}
