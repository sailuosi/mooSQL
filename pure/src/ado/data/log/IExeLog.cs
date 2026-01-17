

namespace mooSQL.data
{
    public  interface IExeLog
    {
         bool IsEnabled(LogLv level);

         void LogDebug(string msg);
        void LogWarning(string v);
        void LogError(string v);
    }
}
