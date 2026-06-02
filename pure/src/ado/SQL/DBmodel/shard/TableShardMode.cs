namespace mooSQL.data
{
    /// <summary>
    /// 分表周期模式。<see cref="None"/> 为默认值，表示不分表。
    /// </summary>
    public enum TableShardMode
    {
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        None = 0,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Year,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Quarter,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Month,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Week,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Day,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Custom,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Interval
    }
}