using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 外键定义
    /// </summary>
    public class DbForeignInfo
    {
        public DbTableInfo Table { get; set; }
        public List<DbColumnInfo> Columns { get; set; } = new List<DbColumnInfo>();
        public DbTableInfo ReferencedTable { get; set; }
        public List<DbColumnInfo> ReferencedColumns { get; set; } = new List<DbColumnInfo>();

    }
}
