
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace mooSQL.data
{
    /// <summary>
    /// 索引定义
    /// </summary>
    public class DbIndexInfo
    {
        /// <summary>
        /// 属性 Name（string）。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 属性 Columns（List<DbIndexColumnInfo>）。
        /// </summary>
        public List<DbIndexColumnInfo> Columns { get; } = new List<DbIndexColumnInfo>();
        /// <summary>
        /// 类型 DbIndexColumnInfo。
        /// </summary>
        public bool IsUnique { get; set; }
    }

    /// <summary>
    /// 索引列
    /// </summary>
    public class DbIndexColumnInfo
    {
        /// <summary>
        /// 属性 Column（DbColumnInfo）。
        /// </summary>
        public DbColumnInfo Column { get; set; }
        /// <summary>
        /// 属性 IsDesc（bool）。
        /// </summary>
        public bool IsDesc { get; set; }
    }
}