namespace mooSQL.data.cluster
{
    /// <summary>
    /// 枚举 FailoverMode。
    /// </summary>
    public enum FailoverMode
    {
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Disabled,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        MarkOnly,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        OnNextConnect,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        ImmediateOnFailure
    }
}