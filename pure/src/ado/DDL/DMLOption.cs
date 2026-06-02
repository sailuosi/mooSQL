using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 类型 DMLOption。
    /// </summary>
    public class DMLOption
    {
        /// <summary>
        /// 字段 IsDropColumn（bool）。
        /// </summary>
        public bool IsDropColumn;

        /// <summary>
        /// 字段 TableNames（string[]）。
        /// </summary>
        public string[] TableNames;
    }
}