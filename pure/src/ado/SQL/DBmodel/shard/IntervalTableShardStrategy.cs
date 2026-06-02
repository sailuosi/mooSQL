using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 按固定间隔（月/日/小时等）分表，对标 FreeSql AsTable 间隔语法。
    /// </summary>
    public class IntervalTableShardStrategy : ITableShardStrategy
    {
        private readonly EntityShardConfig _config;

        /// <summary>
        /// 初始化 IntervalTableShardStrategy（构造）。
        /// </summary>
        public IntervalTableShardStrategy(EntityShardConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 解析Point。
        /// </summary>
        public string ResolvePoint(EntityInfo en, object rowOrNull, DateTime? pointTime)
        {
            var pt = pointTime ?? ShardKeyHelper.ExtractShardTime(en, rowOrNull);
            if (pt == null)
                return en.DbTableName;
            return TableShardNameFormatter.Format(_config, en, pt.Value);
        }

        /// <summary>
        /// 解析Range。
        /// </summary>
        public IReadOnlyList<string> ResolveRange(EntityInfo en, DateTime from, DateTime to)
        {
            if (from > to)
            {
                var t = from;
                from = to;
                to = t;
            }

            var anchor = _config.Anchor == default ? from : _config.Anchor;
            var tables = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var step = _config.IntervalValue <= 0 ? 1 : _config.IntervalValue;
            var unit = (_config.IntervalUnit ?? "month").Trim().ToLowerInvariant();

            var cur = anchor;
            if (cur > from)
            {
                while (cur > from)
                    cur = StepBack(cur, step, unit);
            }

            while (cur <= to)
            {
                if (cur >= from || cur.AddMonths(1) >= from)
                {
                    var name = TableShardNameFormatter.Format(_config, en, cur);
                    if (seen.Add(name))
                        tables.Add(name);
                }
                cur = StepForward(cur, step, unit);
                if (tables.Count > 500)
                    break;
            }

            if (_config.MaxTablesPerQuery.HasValue && tables.Count > _config.MaxTablesPerQuery.Value)
                tables = tables.GetRange(tables.Count - _config.MaxTablesPerQuery.Value, _config.MaxTablesPerQuery.Value);

            return tables;
        }

        /// <summary>
        /// 解析AllTables。
        /// </summary>
        public IReadOnlyList<string> ResolveAllTables(EntityInfo en)
        {
            var anchor = _config.Anchor == default ? DateTime.Today.AddYears(-1) : _config.Anchor;
            return ResolveRange(en, anchor, DateTime.Now);
        }

        private static DateTime StepForward(DateTime cur, int step, string unit)
        {
            switch (unit)
            {
                case "year": return cur.AddYears(step);
                case "day": return cur.AddDays(step);
                case "hour": return cur.AddHours(step);
                default: return cur.AddMonths(step);
            }
        }

        private static DateTime StepBack(DateTime cur, int step, string unit)
        {
            switch (unit)
            {
                case "year": return cur.AddYears(-step);
                case "day": return cur.AddDays(-step);
                case "hour": return cur.AddHours(-step);
                default: return cur.AddMonths(-step);
            }
        }
    }
}