namespace mooSQL.data.health
{
    /// <summary>
    /// 数据库实例健康状态。
    /// </summary>
    public enum DBHealthStatus
    {
        None = 0,
        Available = 1,
        Unavailable = 2,
        Probing = 3
    }
}
