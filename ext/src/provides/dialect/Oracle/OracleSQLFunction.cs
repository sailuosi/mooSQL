using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class OracleSQLFunction:SooSQLFunction
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

        public override string Now()
        {
            return "SYSDATE";
        }

        public override string Year(string FieldSQL)
        {
            return string.Concat("EXTRACT(YEAR FROM", FieldSQL, ")");
        }

        public override string Month(string FieldSQL)
        {
            return string.Concat("EXTRACT(MONTH  FROM", FieldSQL, ")");
        }

        public override string Day(string FieldSQL)
        {
            return string.Concat("EXTRACT(DAY  FROM", FieldSQL, ")");
        }
    }
}
