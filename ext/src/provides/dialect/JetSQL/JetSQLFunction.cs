
using System;

namespace mooSQL.data
{
    /// <summary>
    /// Jet SQL (Access / Excel) 函数方言：LEN, MID, INSTR, NOW, YEAR, MONTH, DAY 等。
    /// </summary>
    public class JetSQLFunction : SooSQLFunction
    {
 /// <summary>
 /// Builds a length expression.
 /// </summary>
 
        public override string Len(string FieldSQL)
        {
            return "LEN(" + FieldSQL + ")";
        }
/// <summary>
/// Builds a substring expression.
/// </summary>
 

        public override string SubStr(string FieldSQL, int start, int length)
        {
            return "MID(" + FieldSQL + "," + start + "," + length + ")";
        }
/// <summary>
/// Builds a character-index expression.
/// </summary>
 

        public override string CharIndex(string subString, string str)
        {
            return "INSTR(" + str + "," + subString + ")";
        }
/// <summary>
/// Builds a ceiling expression.
/// </summary>
 

        public override string Ceil(string FieldSQL)
        {
            return "INT(" + FieldSQL + "+0.5)";
        }
/// <summary>
/// Builds a floor expression.
/// </summary>
 

        public override string Floor(string FieldSQL)
        {
            return "INT(" + FieldSQL + ")";
        }
/// <summary>
/// Builds a round expression.
/// </summary>
 

        public override string Round(string FieldSQL, int decimals)
        {
            return "ROUND(" + FieldSQL + "," + decimals + ")";
        }
/// <summary>
/// Builds a current-timestamp expression.
/// </summary>
 

        public override string Now()
        {
            return "NOW()";
        }
/// <summary>
/// Builds a year-part expression.
/// </summary>
 

        public override string Year(string FieldSQL)
        {
            return "YEAR(" + FieldSQL + ")";
        }
/// <summary>
/// Builds a month-part expression.
/// </summary>
 

        public override string Month(string FieldSQL)
        {
            return "MONTH(" + FieldSQL + ")";
        }
/// <summary>
/// Builds a day-part expression.
/// </summary>
 

        public override string Day(string FieldSQL)
        {
            return "DAY(" + FieldSQL + ")";
        }
/// <summary>
/// Builds a string-concatenation expression.
/// </summary>
 

        public override string Concat(string left, string right)
        {
            return "(" + left + " & " + right + ")";
        }
    }
}
