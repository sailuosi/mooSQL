using System;
using System.Data.Common;

namespace mooSQL.data.health
{
    /// <summary>
    /// 区分连接类异常与 SQL 业务异常。
    /// </summary>
    public static class ConnectionExceptionClassifier
    {
        public static bool IsConnectionError(Exception ex)
        {
            if (ex == null) return false;
            if (ex is DBUnavailableException) return true;
            if (ex is TimeoutException) return true;
            if (ex is InvalidOperationException ioe &&
                ioe.Message != null &&
                (ioe.Message.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 ioe.Message.IndexOf("连接", StringComparison.OrdinalIgnoreCase) >= 0))
                return true;

            var cur = ex;
            while (cur != null)
            {
                if (cur is DbException) return true;
                var name = cur.GetType().Name;
                if (name.IndexOf("Connection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Network", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Socket", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                var msg = cur.Message ?? string.Empty;
                if (msg.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("broken connection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("login failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("unable to connect", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("连接", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                cur = cur.InnerException;
            }
            return false;
        }
    }
}
