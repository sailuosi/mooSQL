namespace mooSQL.data
{
    /// <summary>
    /// SQLBuilder <c>doUpsert</c> 执行策略。
    /// </summary>
    public enum UpsertMode
    {
        /// <summary>
        /// 自动：支持 MERGE 的库走 MERGE；MySQL 族保守走先查后写；其它不支持则先查后写。
        /// </summary>
        Auto = 0,

        /// <summary>
        /// 明确匹配键为主键：MySQL 族启用 ON DUPLICATE KEY UPDATE；其它同 <see cref="Auto"/>。
        /// </summary>
        PrimaryKey = 1,

        /// <summary>
        /// 全部方言强制 EXISTS → update / insert。
        /// </summary>
        CheckExist = 2,

        /// <summary>
        /// 强制 MERGE 构建（MySQL 走 buildMergeIntoFallBack 兼容路径）。
        /// </summary>
        Merge = 3,
    }
}
