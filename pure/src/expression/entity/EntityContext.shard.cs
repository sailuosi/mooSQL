using System;

namespace mooSQL.data
{
    public partial class EntityContext
    {
        public EntityContext ConfigureShard<T>(Action<EntityShardConfig> configure)
        {
            ShardRegistration.ConfigureShard<T>(this, configure);
            return this;
        }

        public EntityContext UseShardStrategy<T>(ITableShardStrategy strategy)
        {
            ShardRegistration.UseShardStrategy<T>(this, strategy);
            return this;
        }

        public EntityContext useShard<T>(Func<T, string> nameParser)
        {
            ShardRegistration.EnableLiveNameWithParser(this, nameParser);
            return this;
        }
    }
}
