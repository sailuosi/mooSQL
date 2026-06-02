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

        /// <summary>
        /// 初始化 TableShardInterceptor（构造）。
        /// </summary>
        public TableShardInterceptor(EntityInfo entity, ITableShardStrategy strategy)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        /// <summary>
        /// 泛型方法 Parse（返回 string）。
        /// </summary>
        public string Parse<T>(T value)
        {
            var pt = ShardKeyHelper.ExtractShardTime(_entity, value);
            return _strategy.ResolvePoint(_entity, value, pt);
        }
    }
}