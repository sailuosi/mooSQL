using System;
using System.Collections.Generic;
using System.Reflection;

namespace mooSQL.data.translation
{
    /// <summary>
    /// DbFunc 方法 → SQL 模板注册表。挂接在 <see cref="Dialect"/>，供 Ext LINQ 编译层查询。
    /// </summary>
    public sealed class DbFuncRegistry
    {
        readonly Dictionary<DbFuncRegistryKey, DbFuncExpressionEntry> _entries = new();
        readonly Dictionary<MemberInfo, DbFuncExpressionEntry> _defaultEntries = new();

        public void Register(MethodInfo method, DbFuncExpressionEntry entry, string? dialectConfiguration = null)
        {
            if (method.IsGenericMethod)
                method = method.GetGenericMethodDefinitionCached();

            if (dialectConfiguration == null)
                _defaultEntries[method] = entry;
            else
                _entries[new DbFuncRegistryKey(method, dialectConfiguration)] = entry;
        }

        public DbFuncExpressionEntry? Resolve(MethodInfo method, string? dialectConfiguration = null)
        {
            if (method.IsGenericMethod)
                method = method.GetGenericMethodDefinitionCached();

            if (dialectConfiguration != null && _entries.TryGetValue(new DbFuncRegistryKey(method, dialectConfiguration), out var specific))
                return specific;

            _defaultEntries.TryGetValue(method, out var def);
            return def;
        }

        public IReadOnlyDictionary<MemberInfo, DbFuncExpressionEntry> DefaultEntries => _defaultEntries;

        readonly struct DbFuncRegistryKey : IEquatable<DbFuncRegistryKey>
        {
            readonly MemberInfo _method;
            readonly string? _dialect;

            public DbFuncRegistryKey(MemberInfo method, string? dialect)
            {
                _method = method;
                _dialect = dialect;
            }

            public override bool Equals(object? obj) => obj is DbFuncRegistryKey other && Equals(other);

            public bool Equals(DbFuncRegistryKey other) =>
                ReferenceEquals(_method, other._method) && _dialect == other._dialect;

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = _method?.GetHashCode() ?? 0;
                    hash = (hash * 397) ^ (_dialect?.GetHashCode() ?? 0);
                    return hash;
                }
            }
        }
    }
}
