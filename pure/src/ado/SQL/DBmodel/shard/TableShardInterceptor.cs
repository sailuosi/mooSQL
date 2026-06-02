using System;

namespace mooSQL.data
{
    /// <summary>
    /// 将 <see cref="ITableShardStrategy"/> 适配为 <see cref="ITableNameInterceptor"/>。
    /// </summary>
    public class TableShardInterceptor : ITableNameInterceptor
    {
        private readonly EntityInfo _entity;
        private readonly ITableShardStrategy _strategy;

        public TableShardInterceptor(EntityInfo entity, ITableShardStrategy strategy)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        public string Parse<T>(T value)
        {
            var pt = ShardKeyHelper.ExtractShardTime(_entity, value);
            return _strategy.ResolvePoint(_entity, value, pt);
        }
    }
}
