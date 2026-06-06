using System.Collections.Generic;
using System.Reflection;

namespace mooSQL.data.translation
{
    /// <summary>
    /// DbFunc 方法 → SQL 模板注册表。挂接在 <see cref="Dialect"/>，供 Ext LINQ 编译层查询。
    /// </summary>
    public sealed class DbFuncRegistry
    {
        readonly Dictionary<(MemberInfo Method, string? Dialect), DbFuncExpressionEntry> _entries = new();
        readonly Dictionary<MemberInfo, DbFuncExpressionEntry> _defaultEntries = new();

        public void Register(MethodInfo method, DbFuncExpressionEntry entry, string? dialectConfiguration = null)
        {
            if (method.IsGenericMethod)
                method = method.GetGenericMethodDefinitionCached();

            if (dialectConfiguration == null)
                _defaultEntries[method] = entry;
            else
                _entries[(method, dialectConfiguration)] = entry;
        }

        public DbFuncExpressionEntry? Resolve(MethodInfo method, string? dialectConfiguration = null)
        {
            if (method.IsGenericMethod)
                method = method.GetGenericMethodDefinitionCached();

            if (dialectConfiguration != null && _entries.TryGetValue((method, dialectConfiguration), out var specific))
                return specific;

            _defaultEntries.TryGetValue(method, out var def);
            return def;
        }

        public IReadOnlyDictionary<MemberInfo, DbFuncExpressionEntry> DefaultEntries => _defaultEntries;
    }
}
