using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 批量复制计数器
    /// </summary>
    public class BulkCopyRowsCopied
    {
        /// <summary>
        /// 中断标识
        /// </summary>
        public bool Abort { get; set; }

        /// <summary>
        /// 已复制数
        /// </summary>
        public long RowsCopied { get; set; }

        /// <summary>
        /// 开始执行时间
        /// </summary>
        public DateTime StartTime { get; } = DateTime.Now;
    }
}
