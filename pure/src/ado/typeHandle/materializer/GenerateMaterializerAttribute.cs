using System;

namespace mooSQL.data
{
    /// <summary>
    /// 标记实体类型在编译期生成 AOT 物化器（源生成器扫描）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class GenerateMaterializerAttribute : Attribute
    {
    }
}
