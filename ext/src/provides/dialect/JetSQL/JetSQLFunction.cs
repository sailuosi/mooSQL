
using System;

namespace mooSQL.data
{
    /// <summary>
    /// Jet SQL (Access / Excel) 函数方言：LEN, MID, INSTR, NOW, YEAR, MONTH, DAY 等。
    /// </summary>
    public class JetSQLFunction : SooSQLFunction
    {
        public override string Len(string FieldSQL)
        {
            return "LEN(" + FieldSQL + ")";
        }

        public override string SubStr(string FieldSQL, int start, int length)
        {
            return "MID(" + FieldSQL + "," + start + "," + length + ")";
        }

        public override string CharIndex(string subString, string str)
        {
            return "INSTR(" + str + "," + subString + ")";
        }

        public override string Ceil(string FieldSQL)
        {
            return "INT(" + FieldSQL + "+0.5)";
        }

        public override string Floor(string FieldSQL)
        {
            return "INT(" + FieldSQL + ")";
        }

        public override string Round(string FieldSQL, int decimals)
        {
            return "ROUND(" + FieldSQL + "," + decimals + ")";
        }

        public override string Now()
        {
            return "NOW()";
        }

        public override string Year(string FieldSQL)
        {
            return "YEAR(" + FieldSQL + ")";
        }

        public override string Month(string FieldSQL)
        {
            return "MONTH(" + FieldSQL + ")";
        }

        public override string Day(string FieldSQL)
        {
            return "DAY(" + FieldSQL + ")";
        }

        public override string Concat(string left, string right)
        {
            return "(" + left + " & " + right + ")";
        }
    }
}
