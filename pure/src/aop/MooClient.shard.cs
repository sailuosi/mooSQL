using System;

namespace mooSQL.data
{
    public partial class MooClient
    {
        /// <summary>
        /// 为实体 <typeparamref name="T"/> 注册 Lambda 分表路由。
        /// </summary>
        public MooClient useShard<T>(Func<T, string> nameParser)
        {
            ShardRegistration.EnableLiveNameWithParser(EntityCash, nameParser);
            return this;
        }

        /// <summary>
        /// 注册完整分表策略（支持范围查询）。
        /// </summary>
        public MooClient useShardStrategy<T>(ITableShardStrategy strategy)
        {
            ShardRegistration.UseShardStrategy<T>(EntityCash, strategy);
            return this;
        }

        /// <summary>
        /// 编程式配置分表（等价于 <see cref="SooTableAttribute"/> 分表属性）。
        /// </summary>
        public MooClient configureShard<T>(Action<EntityShardConfig> configure)
        {
            ShardRegistration.ConfigureShard<T>(EntityCash, configure);
            return this;
        }

        /// <summary>
        /// 注册表名拦截器并启用动态表名。
        /// </summary>
        public MooClient useShard<T>(ITableNameInterceptor interceptor, string name = null)
        {
            var en = EntityCash.getEntityInfo<T>();
            en.LiveName = true;
            en.UseNameParser(string.IsNullOrWhiteSpace(name) ? ShardRegistration.DefaultParserKey : name, interceptor);
            return this;
        }
    }
}
