using System;

namespace mooSQL.data
{
    /// <summary>
    /// 写侧 Accessor 解析：仅 Client 实例表 → 反射。不读任何静态查找表。
    /// </summary>
    internal static class EntityAccessorResolver
    {
        internal static IEntityTypeAccessor Resolve(MooClient client, Type type)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));

            if (client != null && client.TryGetAccessor(type, out var fromClient) && fromClient != null)
                return fromClient;

            return ReflectionEntityAccessor.For(type);
        }
    }
}
