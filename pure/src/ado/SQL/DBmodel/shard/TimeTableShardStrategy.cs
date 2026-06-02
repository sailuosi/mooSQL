using System;
using System.Collections.Generic;
using System.Globalization;

namespace mooSQL.data
{
    /// <summary>
    /// 按年/季/月/周/日的时间分表策略。
    /// </summary>
    public class TimeTableShardStrategy : ITableShardStrategy
    {
        private readonly EntityShardConfig _config;

        public TimeTableShardStrategy(EntityShardConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public string ResolvePoint(EntityInfo en, object rowOrNull, DateTime? pointTime)
        {
            var pt = pointTime ?? ShardKeyHelper.ExtractShardTime(en, rowOrNull);
            if (pt == null)
                return en.DbTableName;
            return TableShardNameFormatter.Format(_config, en, pt.Value);
        }

        public IReadOnlyList<string> ResolveRange(EntityInfo en, DateTime from, DateTime to)
        {
            if (from > to)
            {
                var t = from;
                from = to;
                to = t;
            }

            var tables = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var bucket in EnumerateBuckets(from, to))
            {
                var name = TableShardNameFormatter.Format(_config, en, bucket);
                if (seen.Add(name))
                    tables.Add(name);
            }

            ApplyMaxTables(tables);
            return tables;
        }

        public IReadOnlyList<string> ResolveAllTables(EntityInfo en)
        {
            var anchor = _config.Anchor == default ? DateTime.Today.AddYears(-1) : _config.Anchor;
            return ResolveRange(en, anchor, DateTime.Now);
        }

        private IEnumerable<DateTime> EnumerateBuckets(DateTime from, DateTime to)
        {
            switch (_config.Mode)
            {
                case TableShardMode.Year:
                    for (var y = from.Year; y <= to.Year; y++)
                        yield return new DateTime(y, 1, 1);
                    yield break;
                case TableShardMode.Quarter:
                    var startQ = new DateTime(from.Year, ((from.Month - 1) / 3) * 3 + 1, 1);
                    var endQ = new DateTime(to.Year, ((to.Month - 1) / 3) * 3 + 1, 1);
                    for (var d = startQ; d <= endQ; d = d.AddMonths(3))
                        yield return d;
                    yield break;
                case TableShardMode.Month:
                    for (var d = new DateTime(from.Year, from.Month, 1); d <= to; d = d.AddMonths(1))
                        yield return d;
                    yield break;
                case TableShardMode.Week:
                    var cur = from.Date;
                    while (cur <= to)
                    {
                        yield return cur;
                        cur = cur.AddDays(7);
                    }
                    yield break;
                case TableShardMode.Day:
                    for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
                        yield return d;
                    yield break;
            }
        }

        private void ApplyMaxTables(List<string> tables)
        {
            var max = _config.MaxTablesPerQuery;
            if (max.HasValue && tables.Count > max.Value)
                tables.RemoveRange(max.Value, tables.Count - max.Value);
        }
    }
}
