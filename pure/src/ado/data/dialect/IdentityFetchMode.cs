namespace mooSQL.data
{
    /// <summary>
    /// 插入后取库生成主键的方言策略（见设计文档「插入主键回填与应用发号」§3.6）。
    /// </summary>
    public enum IdentityFetchMode
    {
        /// <summary>同命令 <c>INSERT; SELECT last_insert_id</c>（或降级两趟）。</summary>
        PostSelect,
        /// <summary>同句 <c>RETURNING</c> / <c>OUTPUT</c>。</summary>
        Returning,
        /// <summary>不支持；调用方应收到明确异常。</summary>
        Unsupported
    }
}
