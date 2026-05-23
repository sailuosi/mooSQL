using System;
using System.Collections.Concurrent;
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// 源生成器物化器注册表。生成代码通过 <see cref="Register"/> 或 ModuleInitializer 注册。
    /// </summary>
    public static class MaterializerRegistry
    {
        private static readonly ConcurrentDictionary<Type, Func<DbDataReader, DBInstance, object>> _handlers = new();

        /// <summary>
        /// 注册编译期生成的物化器。
        /// </summary>
        public static void Register(Type type, Func<DbDataReader, DBInstance, object> materializer)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            if (materializer is null) throw new ArgumentNullException(nameof(materializer));
            _handlers[type] = materializer;
        }

        /// <summary>
        /// 尝试获取已注册的物化器。
        /// </summary>
        public static bool TryGet(Type type, out Func<DbDataReader, DBInstance, object> materializer)
            => _handlers.TryGetValue(type, out materializer);

        /// <summary>
        /// 清除所有已注册物化器（主要用于测试）。
        /// </summary>
        public static void Clear() => _handlers.Clear();
    }
}
