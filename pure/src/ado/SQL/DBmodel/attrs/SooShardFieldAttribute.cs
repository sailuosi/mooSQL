using System;

namespace mooSQL.data
{
    /// <summary>
    /// 标记实体分片键字段，对标 SqlSugar <c>[SplitField]</c>。
    /// 须与 <see cref="SooTableAttribute.ShardMode"/> 或 <see cref="MooClient.useShard{T}"/> 配合使用。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class SooShardFieldAttribute : Attribute
    {
    }
}
