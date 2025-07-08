using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 数据库序列
    /// </summary>
    public class DbSequence
    {
        /// <summary>
        /// 数据库序列名
        /// </summary>
        public string SequenceName { get; set; }

        /// <summary>
        /// 序列生成器架构名称。
        /// </summary>
        public string? Schema { get; set; }
    }
}
