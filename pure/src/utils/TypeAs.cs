using System;
using System.Collections.Generic;
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
