// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// 处理期间的消息输出
    /// </summary>
    public class MsgOutput
    {
        /// <summary>
        /// 日志输出事件，参数1为消息内容，参数2为类型（tip,fatal,important,error）
        /// </summary>
        public Action<string, string> onLogging;

        private void doLog(string msg, string type)
        {
            if (onLogging != null) onLogging(msg, type);
        }
        /// <summary>
        /// 提示信息输出，用于显示一些基本信息
        /// </summary>
        /// <param name="msg"></param>
        public void logTip(string msg)
        {
            doLog(msg, "tip");
        }
        /// <summary>
        /// 致命错误信息输出，用于显示一些致命性错误信息
        /// </summary>
        /// <param name="msg"></param>
        public void logFatal(string msg)
        {
            doLog(msg, "fatal");
        }
        /// <summary>
        /// 重要信息输出，用于显示一些重要的基本信息
        /// </summary>
        /// <param name="msg"></param>
        public void logImportant(string msg)
        {
            doLog(msg, "important");
        }
        /// <summary>
        /// 错误信息输出，用于显示一些错误信息
        /// </summary>
        /// <param name="msg"></param>
        public void logError(string msg)
        {
            doLog(msg, "error");
        }
    }
}