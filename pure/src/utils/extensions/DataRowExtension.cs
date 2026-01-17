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

    }
}
