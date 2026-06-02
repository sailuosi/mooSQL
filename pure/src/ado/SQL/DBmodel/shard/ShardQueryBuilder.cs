using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data
{
    /// <summary>
    /// 解析分表查询目标表名列表，并构建 UNION 查询片段。
    /// </summary>
    public static class ShardQueryBuilder
    {
        public static IReadOnlyList<string> ResolveTables(EntityInfo en, ShardQueryOptions options)
        {
            if (en?.Shard == null || !en.Shard.IsActive)
                return new[] { en?.DbTableName ?? string.Empty };

            var strategy = en.Shard.ResolveStrategy();
            if (strategy == null)
                return new[] { en.DbTableName };

            List<string> tables;

            if (options?.AllTables == true)
            {
                tables = strategy.ResolveAllTables(en).ToList();
            }
            else if (options?.RangeFrom != null && options.RangeTo != null)
            {
                tables = strategy.ResolveRange(en, options.RangeFrom.Value, options.RangeTo.Value).ToList();
            }
            else if (options?.InTables != null && options.InTables.Count > 0)
            {
                tables = options.InTables.ToList();
            }
            else
            {
                var recent = options?.TakeRecent ?? en.Shard.DefaultRecentTables;
                if (recent <= 0)
                    recent = 3;
                tables = strategy.ResolveAllTables(en).ToList();
                if (tables.Count > recent)
                    tables = tables.Skip(tables.Count - recent).ToList();
            }

            if (options?.TableFilter != null)
                tables = tables.Where(options.TableFilter).ToList();

            if (tables.Count == 0)
                tables.Add(en.DbTableName);

            return tables;
        }

        /// <summary>
        /// 在 <paramref name="kit"/> 上为每个物理表构建 SELECT 并 UNION ALL。
        /// </summary>
        public static SQLBuilder BuildUnionFrom(
            SQLBuilder kit,
            EntityInfo en,
            ShardQueryOptions options,
            Action<SQLBuilder, string> configureSegment = null)
        {
            var tables = ResolveTables(en, options);
            if (tables.Count == 1)
            {
                kit.from(tables[0]);
                configureSegment?.Invoke(kit, tables[0]);
                return kit;
            }

            var first = true;
            foreach (var tb in tables)
            {
                if (first)
                {
                    kit.from(tb);
                    configureSegment?.Invoke(kit, tb);
                    first = false;
                }
                else
                {
                    kit.union(b =>
                    {
                        b.from(tb);
                        configureSegment?.Invoke(b, tb);
                    });
                }
            }
            return kit;
        }
    }
}
