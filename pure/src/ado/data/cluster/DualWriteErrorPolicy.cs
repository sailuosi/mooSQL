namespace mooSQL.data.cluster
{
    /// <summary>
    /// 枚举 DualWriteErrorPolicy。
    /// </summary>
    public enum DualWriteErrorPolicy
    {
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        MasterWins,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        AllMustSucceed
    }
}