using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data
{
    public partial class SooRepository<T> where T : class, new()
    {
        private DateTime? _shardPoint;

        /// <summary>
        /// 指定本次操作的物理表名。
        /// </summary>
        public SooRepository<T> UseTable(string tableName)
        {
            tbname = tableName;
            return this;
        }

        /// <summary>
        /// 按分片时间点路由单表操作。
        /// </summary>
        public SooRepository<T> ForShard(DateTime pointTime)
        {
            _shardPoint = pointTime;
            if (En.Shard?.ResolveStrategy() is ITableShardStrategy strategy)
                tbname = strategy.ResolvePoint(En, null, pointTime);
            return this;
        }

        /// <summary>
        /// 跨分表范围查询。
        /// </summary>
        public List<T> QueryRange(DateTime from, DateTime to, Action<SQLBuilder> configure = null)
        {
            var kit = DBLive.useSQL();
            kit.fromShardRange<T>(from, to);
            configure?.Invoke(kit);
            return kit.query<T>().ToList();
        }

        protected Func<string> currentTableNameLoader(T entity)
        {
            if (tbname.HasText())
                return tryTableNameLoader(tbname);
            if (_shardPoint.HasValue && En.Shard?.IsActive == true)
            {
                var name = En.Shard.ResolveStrategy()?.ResolvePoint(En, entity, _shardPoint);
                if (!string.IsNullOrWhiteSpace(name))
                    return () => name;
            }
            return null;
        }
    }
}
