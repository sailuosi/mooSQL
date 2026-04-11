using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.utils
{
    /// <summary>
    /// 简单的成功/失败与消息封装（用于翻译器、命令等返回）。
    /// </summary>
    public class StatusResult
    {

        /// <summary>是否成功。</summary>
        public bool Status { get; set; }
        /// <summary>提示或错误信息。</summary>
        public string Message { get; set; }

        /// <summary>
        /// 指定状态与消息构造结果。
        /// </summary>
        public StatusResult(bool status, string message)
        {
            Status = status;
            Message = message;
        }
    }
}
