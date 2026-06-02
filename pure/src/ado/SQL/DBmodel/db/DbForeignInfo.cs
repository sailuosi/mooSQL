using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 外键定义
    /// </summary>
    public class DbForeignInfo
    {
        /// <summary>
        /// 属性 Table（DbTableInfo）。
        /// </summary>
        public DbTableInfo Table { get; set; }
        /// <summary>
        /// 属性 Columns（List<DbColumnInfo>）。
        /// </summary>
        public List<DbColumnInfo> Columns { get; set; } = new List<DbColumnInfo>();
        /// <summary>
        /// 属性 ReferencedTable（DbTableInfo）。
        /// </summary>
        public DbTableInfo ReferencedTable { get; set; }
        /// <summary>
        /// 属性 ReferencedColumns（List<DbColumnInfo>）。
        /// </summary>
        public List<DbColumnInfo> ReferencedColumns { get; set; } = new List<DbColumnInfo>();

    }
}