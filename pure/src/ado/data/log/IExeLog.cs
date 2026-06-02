

namespace mooSQL.data
{
    /// <summary>
    /// 接口 IExeLog。
    /// </summary>
    public  interface IExeLog
    {
         /// <summary>
         /// 内部成员说明。
         /// </summary>
         bool IsEnabled(LogLv level);

         /// <summary>
         /// 内部成员说明。
         /// </summary>
         void LogDebug(string msg);
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        void LogWarning(string v);
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        void LogError(string v);
    }
}