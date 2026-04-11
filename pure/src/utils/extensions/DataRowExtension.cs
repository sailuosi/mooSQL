// 基础功能说明：


using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// 扩展datarow
    /// </summary>
    public static class DataRowExtension
    {
        /// <summary>
        /// 获取一个字符串，或者默认值
        /// </summary>
        /// <param name="row"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static string getString(this DataRow row, string key, string defaultVal)
        {
            if (row == null) return null;
            if (row[key] != null)
            {
                var tar = row[key];
                if (tar == DBNull.Value)
                {
                    return defaultVal;
                }
                if (string.IsNullOrWhiteSpace(tar.ToString()))
                {
                    return defaultVal;
                }
                return tar.ToString();
            }
            return defaultVal;
        }

        /// <summary>
        /// 读取指定列的字符串；DBNull 或缺失列返回 null。
        /// </summary>
        /// <param name="row">数据行。</param>
        /// <param name="key">列名。</param>
        /// <returns>字符串或 null。</returns>
        public static string getString(this DataRow row, string key)
        {
            if (row == null) return null;
            if (row[key] != null)
            {
                var tar = row[key];
                if (tar == DBNull.Value)
                {
                    return null;
                }
                return tar.ToString();
            }
            return null;
        }
        /// <summary>
        /// 获取一个int 或默认值
        /// </summary>
        /// <param name="row"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static int getInt(this DataRow row, string key, int defaultVal)
        {
            var val = row.getString(key, "");
            if (val == "") return defaultVal;
            int v;
            if (int.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }
        /// <summary>
        /// 读取指定列为可空 int；空或解析失败返回 null。
        /// </summary>
        /// <param name="row">数据行。</param>
        /// <param name="key">列名。</param>
        /// <returns>可空整型。</returns>
        public static int? getInt(this DataRow row, string key)
        {
            var val = row.getString(key, "");
            if (val == "") return null;
            int v;
            if (int.TryParse(val, out v))
            {
                return v;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static long? getLong(this DataRow row, string key, long? defaultVal)
        {
            var val = row.getString(key, "");
            if (val == "") return defaultVal;
            long v;
            if (long.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }
        /// <summary>
        /// 读取指定列为可空 long；空或解析失败返回 null。
        /// </summary>
        /// <param name="row">数据行。</param>
        /// <param name="key">列名。</param>
        /// <returns>可空长整型。</returns>
        public static long? getLong(this DataRow row, string key)
        {
            var val = row.getString(key, "");
            if (val == "") return null;
            long v;
            if (long.TryParse(val, out v))
            {
                return v;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static DateTime getDateTime(this DataRow row, string key, DateTime defaultVal)
        {
            var val = row.getString(key, "");
            if (val == "") return defaultVal;
            DateTime v;
            if (DateTime.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }

        /// <summary>
        /// 读取指定列为可空 <see cref="DateTime"/>；空或解析失败返回 null。
        /// </summary>
        /// <param name="row">数据行。</param>
        /// <param name="key">列名。</param>
        /// <returns>可空日期时间。</returns>
        public static DateTime? getDateTime(this DataRow row, string key)
        {
            var val = row.getString(key, "");
            if (val == "") return null;
            DateTime v;
            if (DateTime.TryParse(val, out v))
            {
                return v;
            }
            return null;
        }

        /// <summary>
        /// 失败时返回null
        /// </summary>
        /// <param name="row"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static DateTime? getDateTimeOrNull(this DataRow row, string key)
        {
            var val = row.getString(key, "");
            if (val == "") return null;
            DateTime v;
            if (DateTime.TryParse(val, out v))
            {
                return v;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static DateTime? getDateTime(this DataRow row, string key, DateTime? defaultVal)
        {
            var val = row.getString(key, "");
            if (val == "") return defaultVal;
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
        /// <param name="row"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static double getDouble(this DataRow row, string key, double defaultVal)
        {
            var val = row.getString(key, "");
            if (val == "") return defaultVal;
            double v;
            if (double.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }
        /// <summary>
        /// 读取指定列为可空 double；空或解析失败返回 null。
        /// </summary>
        /// <param name="row">数据行。</param>
        /// <param name="key">列名。</param>
        /// <returns>可空双精度。</returns>
        public static double? getDouble(this DataRow row, string key)
        {
            var val = row.getString(key, "");
            if (val == "") return null;
            double v;
            if (double.TryParse(val, out v))
            {
                return v;
            }
            return null;
        }

        /// <summary>
        /// 读取指定列为 decimal；空或解析失败返回默认值。
        /// </summary>
        /// <param name="row">数据行。</param>
        /// <param name="key">列名。</param>
        /// <param name="defaultVal">解析失败时的默认值。</param>
        /// <returns>decimal 值。</returns>
        public static decimal getDecimal(this DataRow row, string key, decimal defaultVal)
        {
            var val = row.getString(key, "");
            if (val == "") return defaultVal;
            decimal v;
            if (decimal.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }
        /// <summary>
        /// 读取指定列为可空 decimal；空或解析失败返回 null。
        /// </summary>
        /// <param name="row">数据行。</param>
        /// <param name="key">列名。</param>
        /// <returns>可空小数。</returns>
        public static decimal? getDecimal(this DataRow row, string key)
        {
            var val = row.getString(key, "");
            if (val == "") return null;
            decimal v;
            if (decimal.TryParse(val, out v))
            {
                return v;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static Guid getGuid(this DataRow row, string key, Guid defaultVal)
        {
            var val = row.getString(key, "");
            if (val == "") return defaultVal;
            Guid v;
            if (Guid.TryParse(val, out v))
            {
                return v;
            }
            return defaultVal;
        }
        /// <summary>
        /// 读取指定列为可空 <see cref="Guid"/>；空或解析失败返回 null。
        /// </summary>
        /// <param name="row">数据行。</param>
        /// <param name="key">列名。</param>
        /// <returns>可空 GUID。</returns>
        public static Guid? getGuid(this DataRow row, string key)
        {
            var val = row.getString(key, "");
            if (val == "") return null;
            Guid v;
            if (Guid.TryParse(val, out v))
            {
                return v;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="key"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static bool getBoolean(this DataRow row, string key, bool defaultVal)
        {
            var val = row.getString(key, "");
            if (val == "") return defaultVal;
            bool v;
            if (bool.TryParse(val, out v))
            {
                return v;
            }
            //有的数据库使用varchar或int存储布尔值
            if (val.ToLower() == "1"|| val.ToLower().Trim()=="true") {
                return true;
            }
            return defaultVal;
        }
        /// <summary>
        /// 读取指定列为可空 bool；支持 1/true 等常见存储形式。
        /// </summary>
        /// <param name="row">数据行。</param>
        /// <param name="key">列名。</param>
        /// <returns>可空布尔。</returns>
        public static bool? getBoolean(this DataRow row, string key)
        {
            var val = row.getString(key, "");
            if (val == "") return null;
            bool v;
            if (bool.TryParse(val, out v))
            {
                return v;
            }
            //有的数据库使用varchar或int存储布尔值
            if (val.ToLower() == "1" || val.ToLower().Trim() == "true")
            {
                return true;
            }
            return null;
        }
    }
}
