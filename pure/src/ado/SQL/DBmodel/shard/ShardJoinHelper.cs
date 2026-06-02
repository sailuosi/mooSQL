using System;

namespace mooSQL.data
{
    /// <summary>
    /// 分表 JOIN 辅助（Phase D）：分表实体与普通表 / 分表与分表联查的占位扩展。
    /// </summary>
    public static class ShardJoinHelper
    {
        /// <summary>
        /// 构建分表侧子查询 SQL，供手动 join 使用。
        /// </summary>
        public static string BuildShardSubquerySql<T>(
            DBInstance db,
            DateTime from,
            DateTime to,
            string columns = "*")
        {
            var en = db.client.EntityCash.getEntityInfo<T>();
            var kit = db.useSQL();
            kit.select(columns);
            ShardQueryBuilder.BuildUnionFrom(kit, en, ShardQueryOptions.ForRange(from, to));
            return kit.toSelect().toRawSQL(db.dialect.expression.paraPrefix);
        }
    }
}
