using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 映射器工具类，提供类型映射和转换的辅助方法
    /// </summary>
    internal static class MapperUntils
    {
        internal const DbType EnumerableMultiParameter = (DbType)(-1);



        internal static bool ShouldSetDbType(DbType dbType)
            => dbType != EnumerableMultiParameter; // just in case called with non-nullable


        internal const string LinqBinary = "System.Data.Linq.Binary";

        internal const string ObsoleteInternalUsageOnly = "This method is for internal use only";

        internal static readonly MethodInfo
            enumParse = typeof(Enum).GetMethod(nameof(Enum.Parse), new Type[] { typeof(Type), typeof(string), typeof(bool) }),
            getItem = typeof(DbDataReader).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetIndexParameters().Length > 0 && p.GetIndexParameters()[0].ParameterType == typeof(int))
                .Select(p => p.GetGetMethod()).First(),
            getFieldValueT = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue),
                BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(int) }, null),
            isDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull),
                BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(int) }, null);


        internal static readonly MethodInfo format = typeof(ParameterHandler).GetMethod("Format", BindingFlags.Public | BindingFlags.Static);


        internal static readonly MethodInfo StringReplace = typeof(string).GetPublicInstanceMethod(nameof(string.Replace), new Type[] { typeof(string), typeof(string) }),
            InvariantCulture = typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture), BindingFlags.Public | BindingFlags.Static).GetGetMethod();

        internal static int GetColumnHash(DbDataReader reader, int startBound = 0, int length = -1)
        {
            unchecked
            {
                int max = length < 0 ? reader.FieldCount : startBound + length;
                int hash = (-37 * startBound) + max;
                for (int i = startBound; i < max; i++)
                {
                    object tmp = reader.GetName(i);
                    hash = (-79 * ((hash * 31) + (tmp?.GetHashCode() ?? 0))) + (reader.GetFieldType(i)?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        /// <summary>
        /// 仅供内部使用。
        /// </summary>
        /// <param name="value">要转换为字符的对象。</param>

        public static char ReadChar(object value)
        {
            if (value is null || value is DBNull) throw new ArgumentNullException(nameof(value));
            if (value is string s && s.Length == 1) return s[0];
            if (value is char c) return c;
            throw new ArgumentException("A single-character was expected", nameof(value));
        }

        /// <summary>
        /// 仅供内部使用。
        /// </summary>
        /// <param name="value">要转换为字符的对象。</param>
        public static char? ReadNullableChar(object value)
        {
            if (value is null || value is DBNull) return null;
            if (value is string s && s.Length == 1) return s[0];
            if (value is char c) return c;
            throw new ArgumentException("A single-character was expected", nameof(value));
        }


        // one per thread
        [ThreadStatic]
        private static StringBuilder perThreadStringBuilderCache;
        internal static StringBuilder GetStringBuilder()
        {
            var tmp = perThreadStringBuilderCache;
            if (tmp != null)
            {
                perThreadStringBuilderCache = null;
                tmp.Length = 0;
                return tmp;
            }
            return new StringBuilder();
        }

        internal static string ToStringRecycle(this StringBuilder obj)
        {
            if (obj is null) return "";
            var s = obj.ToString();
            if(perThreadStringBuilderCache==null) perThreadStringBuilderCache = obj;
            return s;
        }
        internal static bool IsValueTuple(Type type) => (type?.IsValueType == true
                                               && type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true)
                                               || (type != null && IsValueTuple(Nullable.GetUnderlyingType(type)));


        [SuppressMessage("Style", "IDE0220:Add explicit cast", Justification = "Regex matches are Match")]
        internal static IList<LiteralToken> GetLiteralTokens(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return LiteralToken.None;
            if (!CompiledRegex.LiteralTokens.IsMatch(sql)) return LiteralToken.None;

            var matches = CompiledRegex.LiteralTokens.Matches(sql);
            var found = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<LiteralToken>(matches.Count);
            foreach (Match match in matches)
            {
                string token = match.Value;
                if (found.Add(match.Value))
                {
                    list.Add(new LiteralToken(token, match.Groups[1].Value));
                }
            }
            return list.Count == 0 ? LiteralToken.None : list;
        }

        internal static IEnumerable GetMultiExec(object param)
        {
#pragma warning disable IDE0038 // Use pattern matching - complicated enough!
            return (param is IEnumerable
#pragma warning restore IDE0038 // Use pattern matching
                    && !(param is string
                      || param is IEnumerable<KeyValuePair<string, object>>
                      || param is IDynamicParameters)
                ) ? (IEnumerable)param : null;
        }

        /// <summary>
        /// 抛出数据异常
        /// </summary>
        /// <param name="ex">要抛出的异常。</param>
        /// <param name="index">发生异常的索引。</param>
        /// <param name="reader">发生异常的读取器。</param>
        /// <param name="value">导致异常的值。</param>
        public static void ThrowDataException(Exception ex, int index, IDataReader reader, object value)
        {
            Exception toThrow;
            try
            {
                string name = "(n/a)", formattedValue = "(n/a)";
                if (reader != null && index >= 0 && index < reader.FieldCount)
                {
                    name = reader.GetName(index);
                    if (name == string.Empty)
                    {
                        // Otherwise we throw (=value) below, which isn't intuitive
                        name = "(Unnamed Column)";
                    }
                    try
                    {
                        if (value is null || value is DBNull)
                        {
                            formattedValue = "<null>";
                        }
                        else
                        {
                            formattedValue = Convert.ToString(value) + " - " + Type.GetTypeCode(value.GetType());
                        }
                    }
                    catch (Exception valEx)
                    {
                        formattedValue = valEx.Message;
                    }
                }
                toThrow = new DataException($"解析列时发生错误： {index} ({name}={formattedValue})，参考消息：{ex.Message}", ex);
            }
            catch
            { // throw the **original** exception, wrapped as DataException
                toThrow = new DataException(ex.Message, ex);
            }
            throw toThrow;
        }

        private static readonly int[] ErrTwoRows = new int[2], ErrZeroRows = new int[] { };


    }
}
