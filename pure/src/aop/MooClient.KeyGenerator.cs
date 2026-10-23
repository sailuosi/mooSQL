using System;
using System.Collections.Concurrent;

namespace mooSQL.data
{
    public partial class MooClient
    {
        private readonly ConcurrentDictionary<string, Func<object>> _keyGenerators =
            new ConcurrentDictionary<string, Func<object>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 按字符串 key 注册主键生成委托；列 AutoKey 命中该 key 时调用并写回实体。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 有状态发号（雪花等）：请先进程内 Init 单例，再注册无状态委托，例如
        /// <c>RegisterKeyGenerator&lt;long&gt;("sf", () =&gt; IdHelper.NextId())</c>。
        /// 线程安全由 IdHelper（或等价单例）内部保证；本方法不对 <paramref name="generate"/> 加锁。
        /// </para>
        /// <para>
        /// <b>反模式（极易撞号）</b>：
        /// <c>RegisterKeyGenerator("sf", () =&gt; new MySnowflake(1).Next())</c>
        /// —— 禁止在委托内每次 new 雪花/序列实例。
        /// </para>
        /// </remarks>
        /// <example>
        /// IdHelper.InitIdGenerator(opts);
        /// client.RegisterKeyGenerator&lt;long&gt;("order-snowflake", () =&gt; IdHelper.NextId());
        /// </example>
        public MooClient RegisterKeyGenerator<T>(string key, Func<T> generate)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("key is required.", nameof(key));
            if (generate == null)
                throw new ArgumentNullException(nameof(generate));
            _keyGenerators[key.Trim()] = () => generate();
            return this;
        }

        /// <summary>
        /// 解析 AutoKey：注册表命中，或约定名 Guid 内置 <see cref="Guid.NewGuid"/>；否则抛错。
        /// </summary>
        public object ResolveAutoKey(string autoKey, Type propertyType)
        {
            if (string.IsNullOrWhiteSpace(autoKey))
                return null;

            var key = autoKey.Trim();
            if (_keyGenerators.TryGetValue(key, out var factory))
            {
                var v = factory();
                return CoerceKeyValue(v, propertyType);
            }

            if (string.Equals(key, "Guid", StringComparison.OrdinalIgnoreCase))
            {
                var g = Guid.NewGuid();
                return CoerceKeyValue(g, propertyType);
            }

            throw new InvalidOperationException(
                "Key generator '" + key + "' is not registered. Call RegisterKeyGenerator(\"" + key + "\", ...).");
        }

        /// <summary>是否已注册指定 AutoKey（不含 Guid 内置回退）。</summary>
        public bool HasKeyGenerator(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && _keyGenerators.ContainsKey(key.Trim());
        }

        static object CoerceKeyValue(object value, Type propertyType)
        {
            if (value == null || propertyType == null)
                return value;
            var target = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (target.IsInstanceOfType(value))
                return value;
            if (target == typeof(Guid) && value is string s)
                return Guid.Parse(s);
            return Convert.ChangeType(value, target);
        }
    }
}
