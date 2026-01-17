using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// 类型转换工具
    /// </summary>
    public class TypeAs
    {

        /// <summary>
        /// 转为浮点数
        /// </summary>
        /// <param name="src"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static double asDouble(object src, double defaultVal)
        {
            if (src == null) return defaultVal;
            try
            {
                return Convert.ToDouble(src);
            }
            catch (Exception e)
            {
                return defaultVal;
            }

        }

        /// <summary>
        /// 转换字符串
        /// </summary>
        /// <param name="src"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static string asString(object src, string defaultVal)
        {

            if (src == null)
            {
                return defaultVal;
            }
            if (string.IsNullOrWhiteSpace(src.ToString()))
            {
                return defaultVal;
            }
            return src.ToString();


        }
        /// <summary>
        /// 转换int
        /// </summary>
        /// <param name="src"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static int asInt(object src, int defaultVal)
        {
            var val = asString(src, "");
            int v;
            if (int.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }

        public static long asLong(object src, long defaultVal)
        {
            var val = asString(src, "");
            long v;
            if (long.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static DateTime asDateTime(object src, DateTime defaultVal)
        {
            var val = asString(src, "");
            DateTime v;
            if (DateTime.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }
        // 常见日期格式集合（可根据需求扩展）
        private static readonly string[] CommonFormats = new[]
        {
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "MM/dd/yyyy",
            "dd-MMM-yyyy",
            "yyyyMMdd",
            "yyyy-MM-ddTHH:mm:ss", // ISO 8601
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss",
            "dd-MMM-yyyy HH:mm:ss",
            "yyyyMMddHHmmss",
            "yyyy-MM-ddTHH:mm:ss.fffZ" // ISO 8601 with timezone
        };

        /// <summary>
        /// 安全解析日期字符串
        /// </summary>
        /// <param name="dateString">日期字符串</param>
        /// <param name="result">输出DateTime对象</param>
        /// <param name="culture">可选文化信息</param>
        /// <returns>是否解析成功</returns>
        public static bool asDateTimeFull(string dateString, out DateTime result, CultureInfo culture = null)
        {
            result = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(dateString))
                return false;

            culture = culture ?? CultureInfo.InvariantCulture;

            // 先尝试精确匹配常见格式
            if (DateTime.TryParseExact(dateString, CommonFormats, culture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out result))
            {
                return true;
            }

            // 尝试宽松解析（性能较差，作为备选方案）
            return DateTime.TryParse(dateString, culture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out result);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static DateTime? asDateTime(object src, DateTime? defaultVal)
        {
            var val = asString(src, "");
            DateTime v;
            if (DateTime.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static Guid asGuid(object src, Guid defaultVal)
        {
            var val = asString(src, "");
            Guid v;
            if (Guid.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static bool asBoolean(object src, bool defaultVal)
        {
            var val = asString(src, "");
            bool v;
            if (bool.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }
    }
}
