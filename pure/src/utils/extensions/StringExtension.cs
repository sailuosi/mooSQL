using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public static class StringExtension
    {
        /// <summary>
        /// 判断字符串是否有文本,等效于！IsNullOrWhiteSpace
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool HasText(this string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }
        /// <summary>
        /// 时间为null或0001-01-01 00:00:00时返回true
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static bool IsEmpty(this DateTime? time) { 
            if(time == null) return true;
            if(time == DateTime.MinValue) return true;
            //new DateTime(1900, 1, 1)
            if (time.Value.ToString("yyyy-MM-dd HH:mm:ss") == "1900-01-01 00:00:00") { 
                return true;
            }
            return false;
        }
    }
}
