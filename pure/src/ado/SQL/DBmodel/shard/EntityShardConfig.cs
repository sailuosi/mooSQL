using System;

namespace mooSQL.data
{
    /// <summary>
    /// 实体分表配置，由 <see cref="SooTableAttribute"/> 或 <see cref="ShardRegistration"/> 填充。
    /// </summary>
    public class EntityShardConfig
    {
        public TableShardMode Mode { get; set; }

        /// <summary>分片键属性名，由 <see cref="SooShardFieldAttribute"/> 解析填充。</summary>
        public string ShardKeyProperty { get; set; }

        public EntityColumn ShardKeyColumn { get; set; }

        public string TablePrefix { get; set; }

        public string NameTemplate { get; set; }

        public DateTime Anchor { get; set; }

        public int IntervalValue { get; set; } = 1;

        public string IntervalUnit { get; set; }

        public int? FirstSpan { get; set; }

        public int DefaultRecentTables { get; set; } = 3;

        public int? MaxTablesPerQuery { get; set; }

        public bool AutoCreateOnInsert { get; set; }

        public DayOfWeek WeekStart { get; set; } = DayOfWeek.Monday;

        public Type CustomShardStrategyType { get; set; }

        public ITableShardStrategy CustomStrategy { get; set; }

        public bool IsActive => Mode != TableShardMode.None || CustomStrategy != null;

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
