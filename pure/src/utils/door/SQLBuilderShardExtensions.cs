using System;

namespace mooSQL.data
{
    /// <summary>
    /// SQLBuilder 分表扩展。
    /// </summary>
    public static class SQLBuilderShardExtensions
    {
        /// <summary>
        /// 启用分表构建（对标 SqlSugar .SplitTable()）。
        /// </summary>
        public static SQLBuilder splitTable<T>(this SQLBuilder kit)
        {
            var en = kit.DBLive.client.EntityCash.getEntityInfo<T>();
            kit.ShardSplit = new ShardSplitContext
            {
                EntityType = typeof(T),
                EntityInfo = en,
                Options = new ShardQueryOptions()
            };
            return kit;
        }

        /// <summary>
        /// 泛型方法 splitTable（返回 SQLBuilder）。
        /// </summary>
        public static SQLBuilder splitTable<T>(this SQLBuilder kit, DateTime from, DateTime to)
        {
            kit.splitTable<T>();
            kit.ShardSplit.Options = ShardQueryOptions.ForRange(from, to);
            return kit;
        }

        /// <summary>
        /// takeRecent 方法（返回 SQLBuilder）。
        /// </summary>
        public static SQLBuilder takeRecent(this SQLBuilder kit, int count)
        {
            if (kit.ShardSplit != null)
                kit.ShardSplit.Options.TakeRecent = count;
            return kit;
        }

        /// <summary>
        /// inTables 方法（返回 SQLBuilder）。
        /// </summary>
        public static SQLBuilder inTables(this SQLBuilder kit, params string[] tableNames)
        {
            if (kit.ShardSplit != null)
                kit.ShardSplit.Options.InTables = tableNames;
            return kit;
        }

        /// <summary>
        /// filterTables 方法（返回 SQLBuilder）。
        /// </summary>
        public static SQLBuilder filterTables(this SQLBuilder kit, Func<string, bool> predicate)
        {
            if (kit.ShardSplit != null)
                kit.ShardSplit.Options.TableFilter = predicate;
            return kit;
        }

        /// <summary>
        /// splitAllTables 方法（返回 SQLBuilder）。
        /// </summary>
        public static SQLBuilder splitAllTables(this SQLBuilder kit)
        {
            if (kit.ShardSplit != null)
                kit.ShardSplit.Options.AllTables = true;
            return kit;
        }

        /// <summary>
        /// 按分表范围构建 FROM（UNION ALL）。
        /// </summary>
        public static SQLBuilder fromShardRange<T>(this SQLBuilder kit, DateTime from, DateTime to)
        {
            var en = kit.DBLive.client.EntityCash.getEntityInfo<T>();
            return ShardQueryBuilder.BuildUnionFrom(kit, en, ShardQueryOptions.ForRange(from, to));
        }

        /// <summary>
        /// buildShardFrom 方法（返回 SQLBuilder）。
        /// </summary>
        public static SQLBuilder buildShardFrom(this SQLBuilder kit)
        {
            if (kit.ShardSplit?.EntityInfo == null)
                return kit;
            return ShardQueryBuilder.BuildUnionFrom(kit, kit.ShardSplit.EntityInfo, kit.ShardSplit.Options);
        }
    }
}