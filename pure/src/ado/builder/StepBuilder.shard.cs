namespace mooSQL.data
{
    public partial class StepBuilder
    {
        /// <summary>
        /// 当前构建器的分表上下文；未启用分表时为 null。
        /// </summary>
        public override ShardSplitContext ShardSplit { get; set; }
    }
}
