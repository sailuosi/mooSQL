using System;

namespace mooSQL.data
{
    /// <summary>
    /// 标记实体类型在编译期生成 AOT 读侧物化器与写侧 Accessor（源生成器扫描）。
    /// 生成物须经 <see cref="MooClient.RegisterGeneratedMaterializers"/> /
    /// <see cref="MooClient.RegisterGeneratedAccessors"/> 装入 Client 后生效。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class GenerateMaterializerAttribute : Attribute
    {
    }
}
