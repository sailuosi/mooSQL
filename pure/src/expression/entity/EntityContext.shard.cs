using System;

namespace mooSQL.data
{
    public partial class EntityContext
    {
        /// <summary>
        /// 泛型方法 ConfigureShard（返回 EntityContext）。
        /// </summary>
        public EntityContext ConfigureShard<T>(Action<EntityShardConfig> configure)
        {
            ShardRegistration.ConfigureShard<T>(this, configure);
            return this;
        }

        /// <summary>
        /// 泛型方法 UseShardStrategy（返回 EntityContext）。
        /// </summary>
        public EntityContext UseShardStrategy<T>(ITableShardStrategy strategy)
        {
            ShardRegistration.UseShardStrategy<T>(this, strategy);
            return this;
        }

        /// <summary>
        /// 泛型方法 useShard（返回 EntityContext）。
        /// </summary>
        public EntityContext useShard<T>(Func<T, string> nameParser)
        {
            ShardRegistration.EnableLiveNameWithParser(this, nameParser);
            return this;
        }
    }
}