using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class SQLLiteFunction:SooSQLFunction
    {
        public override string Len(string FieldSQL)
        {
            return "LENGTH(" + FieldSQL + ")";
        }

        public override string SubStr(string FieldSQL, int start, int length)
        {
            return string.Concat("SUBSTR(", FieldSQL, ",", start.ToString(), ",", length.ToString(), ")");
        }

        public override string CharIndex(string subString, string str)
        {
            return string.Concat("INSTR(", str, ",", subString, ")");
        }

        public override string Now() {
            return "DATETIME('now')";
        }

        public override string Year(string FieldSQL)
        {
            return string.Concat("STRFTIME('%Y',", FieldSQL, ")");
        }

        public override string Month(string FieldSQL)
        {
            return string.Concat("STRFTIME('%m',", FieldSQL, ")");
        }

        public override string Day(string FieldSQL)
        {
            return string.Concat("STRFTIME('%d',", FieldSQL, ")");
        }
    }
}
