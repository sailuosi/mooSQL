using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.builder
{
    public class DDLFragSQL
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string Table;

        public string Schema;

        public string TableCaption;
        /// <summary>
        /// 
        /// </summary>
        public List<DDLField> Columns;
        /// <summary>
        /// 0 -- 不检查 1-- 检查
        /// </summary>
        public int CheckIfExists = 0;
        /// <summary>
        /// 0 -- 普通表 1 -- 视图 2 -- 系统表
        /// </summary>
        public int TableType;
        /// <summary>
        /// 查询SQL
        /// </summary>
        public string SelectSQL;

        /// <summary>
        /// 要复制的来源表
        /// </summary>
        public string SrcTable;
        /// <summary>
        /// 索引
        /// </summary>
        public List<DDLIndex> Indexes;
    }
}
