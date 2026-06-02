

namespace mooSQL.data
{
    /// <summary>
    /// 类型 MooLoger。
    /// </summary>
    public class MooLoger:IExeLog
    {

        /// <summary>
        /// 判断是否为Enabled。
        /// </summary>
        public virtual bool IsEnabled(LogLv level) {
            return false;
        }

        /// <summary>
        /// LogDebug 方法。
        /// </summary>
        public virtual void LogDebug(string msg) {

        }

        /// <summary>
        /// LogError 方法。
        /// </summary>
        public virtual void LogError(string v)
        {
            
        }

        /// <summary>
        /// LogWarning 方法。
        /// </summary>
        public virtual void LogWarning(string v)
        {
            
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public enum LogLv {
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Debug = 0,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Product = 1,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Error = 2,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Warning = 3
    }
}