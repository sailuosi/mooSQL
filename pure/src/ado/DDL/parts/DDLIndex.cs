using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 表索引定义
    /// </summary>
    public class DDLIndex
    {
        /// <summary>
        /// 索引名
        /// </summary>
        public string IndexName { get; set; }

        public List<string> MapedFields { get; set; }
        /// <summary>
        /// 添加一个索引字段
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public DDLIndex Add(string fieldName) {
            if (this.MapedFields == null) { 
                MapedFields = new List<string>();
            }
            if (!string.IsNullOrWhiteSpace(fieldName) && !MapedFields.Contains(fieldName)) { 
                MapedFields.Add(fieldName);
            }
            return this;
        }


    }
}
