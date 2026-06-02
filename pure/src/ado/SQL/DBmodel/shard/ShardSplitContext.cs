using System;

namespace mooSQL.data
{
    /// <summary>
    /// 分表查询上下文，挂载在 <see cref="SQLBuilder"/> 上。
    /// </summary>
    public class ShardSplitContext
    {
        /// <summary>
        /// 属性 EntityType（Type）。
        /// </summary>
        public Type EntityType { get; set; }
        /// <summary>
        /// 属性 EntityInfo（EntityInfo）。
        /// </summary>
        public EntityInfo EntityInfo { get; set; }
        /// <summary>
        /// 属性 Options（ShardQueryOptions）。
        /// </summary>
        public ShardQueryOptions Options { get; set; } = new ShardQueryOptions();
    }
}