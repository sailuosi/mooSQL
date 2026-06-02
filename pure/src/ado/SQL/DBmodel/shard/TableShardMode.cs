namespace mooSQL.data
{
    /// <summary>
    /// 分表周期模式。<see cref="None"/> 为默认值，表示不分表。
    /// </summary>
    public enum TableShardMode
    {
        None = 0,
        Year,
        Quarter,
        Month,
        Week,
        Day,
        Custom,
        Interval
    }
}
