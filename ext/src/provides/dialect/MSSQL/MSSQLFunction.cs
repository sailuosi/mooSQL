using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// MSSQL数据库函数类
    /// </summary>
    public class MSSQLFunction:SooSQLFunction
    {
        public override string Len(string FieldSQL)
        {
            return "LEN(" + FieldSQL + ")";
        }

        public override string SubStr(string FieldSQL, int start, int length)
        {
            return string.Concat("SUBSTRING(", FieldSQL, ",", start.ToString(), ",", length.ToString(), ")");
        }

        public override string CharIndex(string subString, string str)
        {
            return string.Concat("CHARINDEX(", subString, ",", str, ")");
        }

        public override string Ceil(string FieldSQL)
        {
            return string.Concat("CEILING(", FieldSQL, ")");
        }

        public override string Now()
        {
            return "GETDATE()";
        }
        public override string Year(string FieldSQL)
        {
            return string.Concat("YEAR(", FieldSQL, ")");
        }

        public override string Month(string FieldSQL)
        {
            return string.Concat("MONTH(", FieldSQL, ")");
        }

        public override string Day(string FieldSQL)
        {
            return string.Concat("DAY(", FieldSQL, ")");
        }
    }
}
