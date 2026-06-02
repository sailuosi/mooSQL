using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 分表查询选项：范围、最近 N 表、表名筛选等。
    /// </summary>
    public class ShardQueryOptions
    {
        public DateTime? RangeFrom { get; set; }
        public DateTime? RangeTo { get; set; }
        public int? TakeRecent { get; set; }
        public IReadOnlyList<string> InTables { get; set; }
        public Func<string, bool> TableFilter { get; set; }
        public bool AllTables { get; set; }

        public static ShardQueryOptions ForRange(DateTime from, DateTime to) =>
            new ShardQueryOptions { RangeFrom = from, RangeTo = to };

        public static ShardQueryOptions Recent(int count) =>
            new ShardQueryOptions { TakeRecent = count };
    }
}
