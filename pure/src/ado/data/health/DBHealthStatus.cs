namespace mooSQL.data.health
{
    /// <summary>
    /// 数据库实例健康状态。
    /// </summary>
    public enum DBHealthStatus
    {
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        None = 0,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Available = 1,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Unavailable = 2,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Probing = 3
    }
}