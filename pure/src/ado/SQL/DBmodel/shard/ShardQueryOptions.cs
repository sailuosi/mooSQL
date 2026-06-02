using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 分表查询选项：范围、最近 N 表、表名筛选等。
    /// </summary>
    public class ShardQueryOptions
    {
        /// <summary>
        /// 属性 RangeFrom（DateTime?）。
        /// </summary>
        public DateTime? RangeFrom { get; set; }
        /// <summary>
        /// 属性 RangeTo（DateTime?）。
        /// </summary>
        public DateTime? RangeTo { get; set; }
        /// <summary>
        /// 属性 TakeRecent（int?）。
        /// </summary>
        public int? TakeRecent { get; set; }
        /// <summary>
        /// 属性 InTables（IReadOnlyList<string>）。
        /// </summary>
        public IReadOnlyList<string> InTables { get; set; }
        /// <summary>
        /// 属性 AllTables（bool）。
        /// </summary>
        public Func<string, bool> TableFilter { get; set; }
        /// <summary>
        /// 属性 AllTables（bool）。
        /// </summary>
        public bool AllTables { get; set; }

        /// <summary>
        /// ForRange 方法（返回 ShardQueryOptions）。
        /// </summary>
        public static ShardQueryOptions ForRange(DateTime from, DateTime to) =>
            new ShardQueryOptions { RangeFrom = from, RangeTo = to };

        /// <summary>
        /// Recent 方法（返回 ShardQueryOptions）。
        /// </summary>
        public static ShardQueryOptions Recent(int count) =>
            new ShardQueryOptions { TakeRecent = count };
    }
}