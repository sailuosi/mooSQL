namespace mooSQL.data.cluster
{
    /// <summary>
    /// 枚举 ReadRoutePolicy。
    /// </summary>
    public enum ReadRoutePolicy
    {
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        MasterOnly,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        RoundRobin,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        WeightedRandom,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        FirstAvailable,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Custom
    }
}