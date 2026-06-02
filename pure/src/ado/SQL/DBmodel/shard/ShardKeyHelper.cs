using System;
using System.Globalization;

namespace mooSQL.data
{
    internal static class ShardKeyHelper
    {
        public static DateTime? ExtractShardTime(EntityInfo en, object row)
        {
            if (row == null || en?.Shard == null)
                return null;

            var propName = en.Shard.ShardKeyProperty;
            if (string.IsNullOrWhiteSpace(propName))
                return null;

            var col = en.GetColumn(propName);
            var pi = col?.PropertyInfo;
            if (pi == null)
                return null;

            var val = pi.GetValue(row);
            return ToDateTime(val);
        }

        public static DateTime? ToDateTime(object val)
        {
            if (val == null)
                return null;
            if (val is DateTime dt)
                return dt;
            if (val is DateTimeOffset dto)
                return dto.DateTime;
            if (val is string s && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed;
            return null;
        }
    }
}
