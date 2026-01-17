

namespace mooSQL.data
{
    public class MooLoger:IExeLog
    {

        public virtual bool IsEnabled(LogLv level) {
            return false;
        }

        public virtual void LogDebug(string msg) {

        }

        public virtual void LogError(string v)
        {
            
        }

        public virtual void LogWarning(string v)
        {
            
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public enum LogLv {
        Debug = 0,
        Product = 1,
        Error = 2,
        Warning = 3
    }
}
