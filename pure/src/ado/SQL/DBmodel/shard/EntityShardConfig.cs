using System;

namespace mooSQL.data
{
    /// <summary>
    /// 实体分表配置，由 <see cref="SooTableAttribute"/> 或 <see cref="ShardRegistration"/> 填充。
    /// </summary>
    public class EntityShardConfig
    {
        /// <summary>
        /// 属性 Mode（TableShardMode）。
        /// </summary>
        public TableShardMode Mode { get; set; }

        /// <summary>分片键属性名，由 <see cref="SooColumnAttribute.Shard"/> 解析填充。</summary>
        public string ShardKeyProperty { get; set; }

        /// <summary>
        /// 属性 ShardKeyColumn（EntityColumn）。
        /// </summary>
        public EntityColumn ShardKeyColumn { get; set; }

        /// <summary>
        /// 属性 TablePrefix（string）。
        /// </summary>
        public string TablePrefix { get; set; }

        /// <summary>
        /// 属性 NameTemplate（string）。
        /// </summary>
        public string NameTemplate { get; set; }

        /// <summary>
        /// 属性 Anchor（DateTime）。
        /// </summary>
        public DateTime Anchor { get; set; }

        /// <summary>
        /// 属性 IntervalValue（int）。
        /// </summary>
        public int IntervalValue { get; set; } = 1;

        /// <summary>
        /// 属性 IntervalUnit（string）。
        /// </summary>
        public string IntervalUnit { get; set; }

        /// <summary>
        /// 属性 FirstSpan（int?）。
        /// </summary>
        public int? FirstSpan { get; set; }

        /// <summary>
        /// 属性 DefaultRecentTables（int）。
        /// </summary>
        public int DefaultRecentTables { get; set; } = 3;

        /// <summary>
        /// 属性 MaxTablesPerQuery（int?）。
        /// </summary>
        public int? MaxTablesPerQuery { get; set; }

        /// <summary>
        /// 属性 AutoCreateOnInsert（bool）。
        /// </summary>
        public bool AutoCreateOnInsert { get; set; }

        /// <summary>
        /// 属性 WeekStart（DayOfWeek）。
        /// </summary>
        public DayOfWeek WeekStart { get; set; } = DayOfWeek.Monday;

        /// <summary>
        /// 属性 CustomShardStrategyType（Type）。
        /// </summary>
        public Type CustomShardStrategyType { get; set; }

        /// <summary>
        /// 属性 CustomStrategy（ITableShardStrategy）。
        /// </summary>
        public ITableShardStrategy CustomStrategy { get; set; }

        /// <summary>
        /// 字段 IsActive（bool）。
        /// </summary>
        public bool IsActive => Mode != TableShardMode.None || CustomStrategy != null;

        /// <summary>
        /// 解析Strategy。
        /// </summary>
        public ITableShardStrategy ResolveStrategy()
        {
            if (CustomStrategy != null)
                return CustomStrategy;
            if (Mode == TableShardMode.Custom && CustomShardStrategyType != null)
            {
                CustomStrategy = (ITableShardStrategy)Activator.CreateInstance(CustomShardStrategyType);
                return CustomStrategy;
            }
            if (Mode == TableShardMode.Interval)
                return new IntervalTableShardStrategy(this);
            if (Mode != TableShardMode.None)
                return new TimeTableShardStrategy(this);
            return null;
        }
    }
}