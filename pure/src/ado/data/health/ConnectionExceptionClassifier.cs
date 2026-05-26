using System;

namespace mooSQL.data.health
{
    /// <summary>
    /// 区分连接类异常与 SQL 业务异常，委托方言白名单判定。
    /// </summary>
    public static class ConnectionExceptionClassifier
    {
        public static bool IsConnectionError(Exception ex, Dialect dialect)
        {
            if (ex == null || dialect == null) return false;
            if (ex is DBUnavailableException) return true;
            return dialect.IsConnectionLost(ex);
        }
    }
}
