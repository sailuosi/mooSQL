using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// mysql的函数定义
    /// </summary>
    public class MySQLFunction:SooSQLFunction
    {
        /// <summary>
        /// 字段长度
        /// </summary>
        /// <param name="FieldSQL"></param>
        /// <returns></returns>
        public override string Len(string FieldSQL)
        {
            return "CHAR_LENGTH(" + FieldSQL + ")";
        }
        /// <summary>
        /// 子字符串
        /// </summary>
        /// <param name="FieldSQL"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public override string SubStr(string FieldSQL, int start, int length)
        {
            return string.Concat("SUBSTRING(", FieldSQL, ",", start.ToString(), ",", length.ToString(), ")");
        }
        /// <summary>
        /// 字符串位置查找
        /// </summary>
        /// <param name="subString"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public override string CharIndex(string subString, string str)
        {
            return string.Concat("POSITION(", subString, ",", str, ")");
        }
        /// <summary>
        /// 当前时间
        /// </summary>
        /// <returns></returns>
        public override string Now()
        {
            return "NOW()";
        }

        public override string Year(string FieldSQL) { 
            return string.Concat("YEAR(", FieldSQL, ")");
        }

        public override string Month(string FieldSQL) { 
            return string.Concat("MONTH(", FieldSQL, ")");
        }

        public override string Day(string FieldSQL) { 
            return string.Concat("DAY(", FieldSQL, ")");
        }
    }
}
