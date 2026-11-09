using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace mooSQL.data
{
    /// <summary>
    /// 基于 PropertyInfo 的写侧访问器（按 Type 缓存）。Client 未注册生成 Accessor 时回退至此。
    /// </summary>
    internal sealed class ReflectionEntityAccessor : IEntityTypeAccessor
    {
        private static readonly ConcurrentDictionary<Type, ReflectionEntityAccessor> _cache =
            new ConcurrentDictionary<Type, ReflectionEntityAccessor>();

        private readonly Dictionary<string, PropertyInfo> _props;

        private ReflectionEntityAccessor(Type type)
        {
            _props = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
            foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                _props[p.Name] = p;
            }
        }

        internal static ReflectionEntityAccessor For(Type type)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            return _cache.GetOrAdd(type, static t => new ReflectionEntityAccessor(t));
        }

        public object ReadValue(object entity, string propertyName)
        {
            if (entity is null || string.IsNullOrEmpty(propertyName)) return null;
            if (!_props.TryGetValue(propertyName, out var prop)) return null;
            return prop.GetValue(entity);
        }

        public void WriteValue(object entity, string propertyName, object value)
        {
            if (entity is null || string.IsNullOrEmpty(propertyName)) return;
            if (!_props.TryGetValue(propertyName, out var prop) || !prop.CanWrite) return;
            prop.SetValue(entity, value);
        }

        public bool IsNull(object entity, string propertyName)
            => ReadValue(entity, propertyName) == null;
    }
}
