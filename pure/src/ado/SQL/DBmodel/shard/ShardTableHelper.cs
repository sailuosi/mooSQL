using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data
{
    /// <summary>
    /// 分表辅助方法，对标 SqlSugar SplitHelper。
    /// </summary>
    public class ShardTableHelper
    {
        private readonly EntityInfo _en;
        private readonly ITableShardStrategy _strategy;

        public ShardTableHelper(EntityInfo en)
        {
            _en = en ?? throw new ArgumentNullException(nameof(en));
            _strategy = en.Shard?.ResolveStrategy();
        }

        public IReadOnlyList<string> GetTables()
        {
            if (_strategy == null)
                return new[] { _en.DbTableName };
            return _strategy.ResolveAllTables(_en);
        }

        public string GetTableName(DateTime pointTime)
        {
            if (_strategy == null)
                return _en.DbTableName;
            return _strategy.ResolvePoint(_en, null, pointTime);
        }

        public string GetTableName<T>(T entity) where T : class
        {
            if (_strategy == null)
                return _en.DbTableName;
            return _strategy.ResolvePoint(_en, entity, ShardKeyHelper.ExtractShardTime(_en, entity));
        }

        public IReadOnlyList<string> GetTableNames<T>(IEnumerable<T> entities) where T : class
        {
            return entities?
                .Select(e => GetTableName(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();
        }

        public Dictionary<string, List<T>> GroupByTable<T>(IEnumerable<T> entities) where T : class
        {
            var map = new Dictionary<string, List<T>>(StringComparer.OrdinalIgnoreCase);
            if (entities == null)
                return map;
            foreach (var e in entities)
            {
                var tb = GetTableName(e);
                if (!map.TryGetValue(tb, out var list))
                {
                    list = new List<T>();
                    map[tb] = list;
                }
                list.Add(e);
            }
            return map;
        }
    }

    /// <summary>
    /// <see cref="EntityContext"/> 扩展：获取分表辅助类。
    /// </summary>
    public static class ShardTableHelperExtensions
    {
        public static ShardTableHelper GetShardHelper<T>(this EntityContext ctx)
        {
            var en = ctx.getEntityInfo<T>();
            return new ShardTableHelper(en);
        }
    }
}
