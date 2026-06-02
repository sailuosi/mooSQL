using System;
using mooSQL.data.clip;

namespace mooSQL.data
{
    public partial class SQLClip
    {
        /// <summary>
        /// 按时间范围绑定分表实体（UNION ALL 子查询作为 FROM）。
        /// </summary>
        public SQLClip fromShardRange<T>(DateTime from, DateTime to, out T table) where T : new()
        {
            var tar = new T();
            table = tar;
            var en = DBLive.client.EntityCash.getEntityInfo<T>();
            var tool = DBLive.useSQL();
            tool.splitTable<T>(from, to);
            ShardQueryBuilder.BuildUnionFrom(tool, en, ShardQueryOptions.ForRange(from, to));
            tool.select("*");
            var subSql = tool.toSelect().toRawSQL(tool.DBLive.dialect.expression.paraPrefix);

            var alias = string.IsNullOrWhiteSpace(en.Alias) ? "shard_u" : en.Alias;
            var bt = new ClipTable
            {
                BindValue = tar,
                EnityType = typeof(T),
                TableInfo = en,
                BType = ClipTableType.FromBy,
                BSrc = ClipTableSrc.SubSQL,
                querySQL = subSql,
                Alias = alias
            };
            Context.BindFrom(tar, bt);
            return this;
        }
    }
}
