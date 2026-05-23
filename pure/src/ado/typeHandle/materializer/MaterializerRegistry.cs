using System;
using System.Collections.Concurrent;
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// 源生成器物化器静态引导表（进程级）。SG 通过 ModuleInitializer 写入；
    /// 运行时优先使用 <see cref="MooClient"/> 实例注册表，未命中再回退到此表。
    /// </summary>
    public static class MaterializerRegistry
    {
        private static readonly ConcurrentDictionary<Type, Func<DbDataReader, DBInstance, object>> _handlers = new();

        /// <summary>
        /// 注册编译期生成的物化器到静态引导表。
        /// </summary>
        public static void Register(Type type, Func<DbDataReader, DBInstance, object> materializer)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            if (materializer is null) throw new ArgumentNullException(nameof(materializer));
            _handlers[type] = materializer;
        }

        /// <summary>
        /// 尝试从静态引导表获取物化器。
        /// </summary>
        public static bool TryGet(Type type, out Func<DbDataReader, DBInstance, object> materializer)
            => _handlers.TryGetValue(type, out materializer);

        /// <summary>
        /// 将静态引导表中的物化器复制到 <see cref="MooClient"/> 实例注册表。
        /// </summary>
        internal static void CopyTo(MooClient client)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            var copied = false;
            foreach (var pair in _handlers)
            {
                client.RegisterMaterializerImpl(pair.Key, pair.Value, purgeCache: false);
                copied = true;
            }
            if (copied)
                MapperCache.PurgeQueryCache();
        }

        /// <summary>
        /// 清除静态引导表（主要用于测试）。
        /// </summary>
        public static void Clear() => _handlers.Clear();
    }
}
