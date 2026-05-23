using System;
using System.Data.Common;

namespace mooSQL.data
{
    internal static class MaterializerResolver
    {
        internal static Func<DbDataReader, DBInstance, object> Resolve(
            PackUp packUp,
            Type type,
            DbDataReader reader,
            int startBound,
            int length,
            bool returnNullIfFirstMissing)
        {
#if NET6_0_OR_GREATER || NET8_0_OR_GREATER || NET10_0_OR_GREATER
            if (MaterializerRegistry.TryGet(type, out var generated))
                return generated;
#endif
            if (ReflectionMaterializer.TryCreate(packUp, type, startBound, length, returnNullIfFirstMissing, out var reflected))
                return reflected;

            throw new InvalidOperationException(
                $"无法为类型 {type.FullName} 创建 AOT 物化器。请添加 [GenerateMaterializer] 或确保存在无参构造与可写成员。");
        }
    }
}
