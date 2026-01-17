
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
        public string Name { get; set; }
        public List<DbIndexColumnInfo> Columns { get; } = new List<DbIndexColumnInfo>();
        public bool IsUnique { get; set; }
    }

    /// <summary>
    /// 索引列
    /// </summary>
    public class DbIndexColumnInfo
    {
        public DbColumnInfo Column { get; set; }
        public bool IsDesc { get; set; }
    }
}