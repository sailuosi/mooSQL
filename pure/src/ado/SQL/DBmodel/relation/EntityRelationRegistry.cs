using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 客户端级实体关系注册表（双向）；对标 CRL AbsPropertyBuilder.relationCahe，但非静态全局。
    /// </summary>
    public sealed class EntityRelationRegistry
    {
        readonly ConcurrentDictionary<string, EntityRelationInfo> _map =
            new ConcurrentDictionary<string, EntityRelationInfo>(StringComparer.Ordinal);

        /// <summary>已注册条目数（含双向）。</summary>
        public int Count => _map.Count;

        /// <summary>按类型对查找；无则 null。</summary>
        public EntityRelationInfo Find(Type type1, Type type2)
        {
            if (type1 == null || type2 == null) return null;
            _map.TryGetValue(EntityRelationInfo.MakeKey(type1, type2), out var info);
            return info;
        }

        /// <summary>
        /// 注册双向关系。已存在的键跳过（先写入者有效）。
        /// </summary>
        public void RegisterBidirectional(EntityRelationInfo forward)
        {
            if (forward == null) throw new ArgumentNullException(nameof(forward));
            if (forward.Type1 == null || forward.Type2 == null)
                throw new ArgumentException("Relation 两侧类型不能为空。", nameof(forward));
            if (string.IsNullOrEmpty(forward.Field1Name) || string.IsNullOrEmpty(forward.Field2Name))
                throw new ArgumentException("Relation 两侧字段名不能为空。", nameof(forward));

            _map.TryAdd(forward.Key, forward);

            var reverse = new EntityRelationInfo
            {
                Type1 = forward.Type2,
                Type2 = forward.Type1,
                Field1Name = forward.Field2Name,
                Field2Name = forward.Field1Name,
                Expression = forward.Expression,
                Parameters = forward.Parameters
            };
            _map.TryAdd(reverse.Key, reverse);
        }

        /// <summary>是否已注册该方向。</summary>
        public bool Contains(Type type1, Type type2)
            => Find(type1, type2) != null;

        /// <summary>与指定类型相关的全部关系（任意一侧）。</summary>
        public IEnumerable<EntityRelationInfo> FindInvolving(Type type)
        {
            if (type == null) yield break;
            foreach (var kv in _map)
            {
                var info = kv.Value;
                if (info.Type1 == type || info.Type2 == type)
                    yield return info;
            }
        }

        /// <summary>清空（测试用）。</summary>
        public void Clear() => _map.Clear();
    }
}
