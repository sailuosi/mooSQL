using System;

namespace mooSQL.data
{
    /// <summary>
    /// 分表查询上下文，挂载在 <see cref="SQLBuilder"/> 上。
    /// </summary>
    public class ShardSplitContext
    {
        public Type EntityType { get; set; }
        public EntityInfo EntityInfo { get; set; }
        public ShardQueryOptions Options { get; set; } = new ShardQueryOptions();
    }
}
