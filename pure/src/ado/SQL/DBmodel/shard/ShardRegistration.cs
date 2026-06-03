using System;
using mooSQL.data.Mapping;

namespace mooSQL.data
{
    /// <summary>
    /// 分表注册：特性解析、<see cref="MooClient.useShard"/>、拦截器挂载。
    /// </summary>
    public static class ShardRegistration
    {
        /// <summary>
        /// 默认分表解析占位符键名。
        /// </summary>
        public const string DefaultParserKey = "@shard";

        /// <summary>
        /// ApplyTableAttribute 方法。
        /// </summary>
        public static void ApplyTableAttribute(EntityInfo info, SooTableAttribute attr)
        {
            if (info == null || attr == null || attr.ShardMode == TableShardMode.None)
                return;

            info.Shard ??= new EntityShardConfig();
            var shard = info.Shard;
            shard.Mode = attr.ShardMode;
            shard.NameTemplate = attr.NameTemplate;
            shard.TablePrefix = ExtractTablePrefix(attr.Name, info.DbTableName, info.EntityName);
            shard.DefaultRecentTables = attr.DefaultRecentTables > 0 ? attr.DefaultRecentTables : 3;
            shard.MaxTablesPerQuery = attr.MaxTablesPerQuery;
            shard.AutoCreateOnInsert = attr.AutoCreateOnInsert;
            shard.IntervalValue = attr.ShardIntervalValue > 0 ? attr.ShardIntervalValue : 1;
            shard.IntervalUnit = attr.ShardIntervalUnit;
            shard.FirstSpan = attr.ShardFirstSpan;
            shard.CustomShardStrategyType = attr.CustomShardStrategyType;

            if (!string.IsNullOrWhiteSpace(attr.ShardAnchor) &&
                DateTime.TryParse(attr.ShardAnchor, out var anchor))
                shard.Anchor = anchor;

            if (attr.LiveName || attr.ShardMode != TableShardMode.None)
                info.LiveName = true;
        }

        /// <summary>
        /// MarkShardField 方法。
        /// </summary>
        public static void MarkShardField(EntityInfo info, EntityColumn column)
        {
            if (info == null || column == null)
                return;
            column.IsShardField = true;
            info.Shard ??= new EntityShardConfig();
            if (string.IsNullOrWhiteSpace(info.Shard.ShardKeyProperty))
            {
                info.Shard.ShardKeyProperty = column.PropertyName;
                info.Shard.ShardKeyColumn = column;
            }
        }

        /// <summary>
        /// FinalizeEntityShard 方法。
        /// </summary>
        public static void FinalizeEntityShard(EntityInfo info)
        {
            if (info?.Shard == null || !info.Shard.IsActive)
                return;

            if (info.Shard.Mode != TableShardMode.Custom &&
                string.IsNullOrWhiteSpace(info.Shard.ShardKeyProperty))
                throw new InvalidOperationException(
                    $"实体 {info.EntityName} 启用了分表但未在 [SooColumn(Shard = true)] 中标记分片键属性。");

            if (info.LiveName != true)
                info.LiveName = true;

            RegisterInterceptor(info, info.Shard.ResolveStrategy());
        }

        /// <summary>
        /// RegisterInterceptor 方法。
        /// </summary>
        public static void RegisterInterceptor(EntityInfo info, ITableShardStrategy strategy)
        {
            if (info == null || strategy == null)
                return;
            info.NameParses[DefaultParserKey] = new TableShardInterceptor(info, strategy);
        }

        /// <summary>
        /// 泛型方法 EnableLiveNameWithParser（返回 void）。
        /// </summary>
        public static void EnableLiveNameWithParser<T>(EntityContext ctx, Func<T, string> nameParser)
        {
            var en = ctx.getEntityInfo<T>();
            en.LiveName = true;
            en.Shard ??= new EntityShardConfig { Mode = TableShardMode.Custom };
            var interceptor = new FuncTableNameCepter(nameParser);
            en.NameParses[DefaultParserKey] = interceptor;
        }

        /// <summary>
        /// 泛型方法 ConfigureShard（返回 void）。
        /// </summary>
        public static void ConfigureShard<T>(EntityContext ctx, Action<EntityShardConfig> configure)
        {
            var en = ctx.getEntityInfo<T>();
            en.Shard ??= new EntityShardConfig();
            configure?.Invoke(en.Shard);
            if (en.Shard.Mode == TableShardMode.None && en.Shard.CustomStrategy == null)
                return;
            en.LiveName = true;
            FinalizeEntityShard(en);
        }

        /// <summary>
        /// 泛型方法 UseShardStrategy（返回 void）。
        /// </summary>
        public static void UseShardStrategy<T>(EntityContext ctx, ITableShardStrategy strategy)
        {
            var en = ctx.getEntityInfo<T>();
            en.LiveName = true;
            en.Shard ??= new EntityShardConfig { Mode = TableShardMode.Custom };
            en.Shard.CustomStrategy = strategy;
            RegisterInterceptor(en, strategy);
        }

        private static string ExtractTablePrefix(string name, string dbTableName, string entityName)
        {
            var raw = name ?? dbTableName ?? entityName;
            if (string.IsNullOrWhiteSpace(raw))
                return entityName;
            var idx = raw.IndexOf('{');
            return idx > 0 ? raw.Substring(0, idx).TrimEnd('_') : raw;
        }
    }
}