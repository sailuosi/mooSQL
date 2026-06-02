namespace mooSQL.data
{
    public partial class SQLBuilder
    {
        /// <summary>
        /// 当前构建器的分表上下文；未启用分表时为 null。
        /// </summary>
        public ShardSplitContext ShardSplit { get; set; }
    }
}
