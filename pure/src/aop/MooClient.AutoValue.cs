using System;
using System.Collections.Concurrent;

namespace mooSQL.data
{
    public partial class MooClient
    {
        private readonly ConcurrentDictionary<string, Func<object>> _autoValueGenerators =
            new ConcurrentDictionary<string, Func<object>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 按字符串 key 注册 AutoValue 生成委托；列 AutoValueInsert / AutoValueUpdate 命中该 key 时调用并写回实体。
        /// </summary>
        /// <example>
        /// client.RegisterAutoValue("user", () =&gt; currentUserId);
        /// </example>
        public MooClient RegisterAutoValue<T>(string key, Func<T> generate)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("key is required.", nameof(key));
            if (generate == null)
                throw new ArgumentNullException(nameof(generate));
            _autoValueGenerators[key.Trim()] = () => generate();
            return this;
        }

        /// <summary>是否已注册指定 AutoValue key（不含内置 true/false/0/1/now）。</summary>
        public bool HasAutoValue(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && _autoValueGenerators.ContainsKey(key.Trim());
        }

        /// <summary>
        /// 解析 AutoValue：注册表命中，或内置 <c>true</c>/<c>false</c>/<c>0</c>/<c>1</c>/<c>now</c>/<c>now()</c>；否则抛错。
        /// </summary>
        public object ResolveAutoValue(string expr, Type propertyType)
        {
            if (string.IsNullOrWhiteSpace(expr))
                return null;

            var key = expr.Trim();
            if (_autoValueGenerators.TryGetValue(key, out var factory))
            {
                var v = factory();
                return CoerceKeyValue(v, propertyType);
            }

            if (TryResolveBuiltinAutoValue(key, out var builtin))
                return CoerceKeyValue(builtin, propertyType);

            throw new InvalidOperationException(
                "AutoValue '" + key + "' is not registered. Call RegisterAutoValue(\"" + key + "\", ...).");
        }

        static bool TryResolveBuiltinAutoValue(string key, out object value)
        {
            value = null;
            if (string.Equals(key, "true", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }
            if (string.Equals(key, "false", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }
            if (key == "1")
            {
                value = 1;
                return true;
            }
            if (key == "0")
            {
                value = 0;
                return true;
            }
            if (string.Equals(key, "now", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "now()", StringComparison.OrdinalIgnoreCase))
            {
                value = DateTime.Now;
                return true;
            }
            return false;
        }
    }
}
