using System;
using System.Linq;

namespace mooSQL.data
{
    /// <summary>
    /// 分表 DDL：同步结构、插入前建表。
    /// </summary>
    public static class ShardDdlHelper
    {
        /// <summary>
        /// 将实体结构同步到指定时间范围内的所有分表。
        /// </summary>
        public static void InitShardTables<T>(DBInstance db, DateTime from, DateTime to)
        {
            var en = db.client.EntityCash.getEntityInfo<T>();
            var creator = new DBTableCreator { DBLive = db, CreateMode = "createAuto" };
            if (en.Shard == null || !en.Shard.IsActive)
            {
                creator.createTable<T>();
                return;
            }

            var tables = ShardQueryBuilder.ResolveTables(en, ShardQueryOptions.ForRange(from, to));
            foreach (var tb in tables)
                creator.createTable<T>(tb);
        }

        /// <summary>
        /// 插入前若表不存在则创建（需 <see cref="EntityShardConfig.AutoCreateOnInsert"/>）。
        /// </summary>
        public static void EnsureTableForInsert(DBInstance db, EntityInfo en, object entity)
        {
            if (en?.Shard == null || !en.Shard.AutoCreateOnInsert)
                return;

            var strategy = en.Shard.ResolveStrategy();
            if (strategy == null)
                return;

            var tb = strategy.ResolvePoint(en, entity, ShardKeyHelper.ExtractShardTime(en, entity));
            if (string.IsNullOrWhiteSpace(tb))
                return;

            var ddl = db.useDDL();
            if (!ddl.hasTable(tb))
                new DBTableCreator { DBLive = db, CreateMode = "createAuto" }.CreateTable(en, tb, "createAuto");
        }
    }
}
