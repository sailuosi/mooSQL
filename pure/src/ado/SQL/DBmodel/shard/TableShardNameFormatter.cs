using System;
using System.Globalization;

namespace mooSQL.data
{
    /// <summary>
    /// 根据模板与时间点格式化为物理表名。
    /// </summary>
    public static class TableShardNameFormatter
    {
        /// <summary>
        /// Format 方法（返回 string）。
        /// </summary>
        public static string Format(EntityShardConfig shard, EntityInfo en, DateTime point)
        {
            var prefix = shard?.TablePrefix;
            if (string.IsNullOrWhiteSpace(prefix))
                prefix = en?.DbTableName ?? "Table";

            var template = shard?.NameTemplate;
            if (string.IsNullOrWhiteSpace(template))
                template = DefaultTemplate(shard?.Mode ?? TableShardMode.Month, prefix);

            return ApplyTemplate(template, prefix, point, shard?.Mode ?? TableShardMode.Month);
        }

        /// <summary>
        /// DefaultTemplate 方法（返回 string）。
        /// </summary>
        public static string DefaultTemplate(TableShardMode mode, string prefix)
        {
            switch (mode)
            {
                case TableShardMode.Year: return prefix + "_{year}";
                case TableShardMode.Quarter: return prefix + "_{year}Q{quarter}";
                case TableShardMode.Month: return prefix + "_{year}{month}";
                case TableShardMode.Week: return prefix + "_{year}W{week}";
                case TableShardMode.Day: return prefix + "_{year}{month}{day}";
                default: return prefix + "_{year}{month}";
            }
        }

        /// <summary>
        /// ApplyTemplate 方法（返回 string）。
        /// </summary>
        public static string ApplyTemplate(string template, string prefix, DateTime point, TableShardMode mode)
        {
            if (string.IsNullOrWhiteSpace(template))
                template = DefaultTemplate(mode, prefix);

            var ci = CultureInfo.InvariantCulture;
            var quarter = (point.Month - 1) / 3 + 1;
            var week = GetIsoWeekOfYear(point);

            var name = template
                .Replace("{prefix}", prefix)
                .Replace("{year}", point.ToString("yyyy", ci))
                .Replace("{month}", point.ToString("MM", ci))
                .Replace("{day}", point.ToString("dd", ci))
                .Replace("{quarter}", quarter.ToString(ci))
                .Replace("{week}", week.ToString("00", ci));

            if (!template.Contains("{") && !name.StartsWith(prefix, StringComparison.Ordinal))
                name = prefix + name;

            return name;
        }

        private static int GetIsoWeekOfYear(DateTime point)
        {
#if NET6_0_OR_GREATER
            return System.Globalization.ISOWeek.GetWeekOfYear(point);
#else
            var cal = CultureInfo.InvariantCulture.Calendar;
            return cal.GetWeekOfYear(point, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
#endif
        }
    }
}