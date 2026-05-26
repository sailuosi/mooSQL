using System;
using System.Data.Common;
using System.Reflection;

namespace mooSQL.data
{
    public abstract partial class SQLSentence
    {
        /// <summary>
        /// 判定异常是否表示数据库连接失联（白名单机制）。
        /// 仅当异常明确匹配本库失联特征时返回 true；默认 false。
        /// </summary>
        public virtual bool IsConnectionLost(Exception ex) => false;

        protected static bool MatchInnerErrorNumber(Exception ex, params int[] numbers)
        {
            if (ex == null || numbers == null || numbers.Length == 0) return false;
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                var n = TryGetErrorNumber(cur);
                if (!n.HasValue) continue;
                foreach (var num in numbers)
                {
                    if (n.Value == num) return true;
                }
            }
            return false;
        }

        protected static bool MatchInnerSqlState(Exception ex, params string[] states)
        {
            if (ex == null || states == null || states.Length == 0) return false;
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                var state = TryGetSqlState(cur);
                if (string.IsNullOrEmpty(state)) continue;
                foreach (var s in states)
                {
                    if (string.IsNullOrEmpty(s)) continue;
                    if (state.Equals(s, StringComparison.OrdinalIgnoreCase)) return true;
                    if (s.Length <= 5 && state.StartsWith(s, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }

        protected static bool MatchMessage(Exception ex, params string[] substrings)
        {
            if (ex == null || substrings == null || substrings.Length == 0) return false;
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                var msg = cur.Message;
                if (string.IsNullOrEmpty(msg)) continue;
                foreach (var s in substrings)
                {
                    if (!string.IsNullOrEmpty(s) &&
                        msg.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            return false;
        }

        protected static bool MatchOracleError(Exception ex, params int[] oraNumbers)
        {
            if (ex == null || oraNumbers == null || oraNumbers.Length == 0) return false;
            if (MatchInnerErrorNumber(ex, oraNumbers)) return true;
            foreach (var n in oraNumbers)
            {
                var tag = "ORA-" + n;
                if (MatchMessage(ex, tag)) return true;
            }
            return false;
        }

        private static int? TryGetErrorNumber(Exception ex)
        {
            if (ex == null) return null;
            var t = ex.GetType();
            var prop = t.GetProperty("Number", BindingFlags.Public | BindingFlags.Instance)
                ?? t.GetProperty("ErrorCode", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                try
                {
                    var v = prop.GetValue(ex, null);
                    if (v is int i) return i;
                    if (v is uint u) return unchecked((int)u);
                    if (v is short s) return s;
                }
                catch
                {
                    /* ignore reflection failures */
                }
            }
            if (ex is DbException dbEx) return dbEx.ErrorCode;
            return null;
        }

        private static string TryGetSqlState(Exception ex)
        {
            if (ex == null) return null;
            var prop = ex.GetType().GetProperty("SqlState", BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return null;
            try
            {
                return prop.GetValue(ex, null) as string;
            }
            catch
            {
                return null;
            }
        }
    }
}
