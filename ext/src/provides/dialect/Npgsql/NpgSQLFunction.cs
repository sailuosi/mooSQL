using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// NpgSQLFunction type.
    /// </summary>
    /// <summary>
    /// NpgSQLFunction type.
    /// </summary>
    public class NpgSQLFunction : SooSQLFunction
    {
 /// <summary>
 /// Builds a length expression.
 /// </summary>
 
        public override string Len(string FieldSQL)
        {
            return "LENGTH(" + FieldSQL + ")";
        }
/// <summary>
/// Builds a substring expression.
/// </summary>
 

        public override string SubStr(string FieldSQL, int start, int length)
        {
            return string.Concat("SUBSTRING(", FieldSQL, ",", start.ToString(), ",", length.ToString(), ")");
        }
/// <summary>
/// Builds a character-index expression.
/// </summary>
 

        public override string CharIndex(string subString, string str)
        {
            return string.Concat("POSITION(", subString, " IN ", str, ")");
        }
/// <summary>
/// Builds a current-timestamp expression.
/// </summary>
 

        public override string Now() { 
            return "CURRENT_TIMESTAMP";
        }
/// <summary>
/// Builds a year-part expression.
/// </summary>
 

        public override string Year(string FieldSQL)
        {
            return string.Concat("EXTRACT(YEAR FROM", FieldSQL, ")");
        }
/// <summary>
/// Builds a month-part expression.
/// </summary>
 

        public override string Month(string FieldSQL)
        {
            return string.Concat("EXTRACT(MONTH  FROM", FieldSQL, ")");
        }
/// <summary>
/// Builds a day-part expression.
/// </summary>
 

        public override string Day(string FieldSQL)
        {
            return string.Concat("EXTRACT(DAY  FROM", FieldSQL, ")");
        }
/// <summary>
/// Builds a string-concatenation expression.
/// </summary>
 

        public override string Concat(string left, string right)
        {
            return string.Concat(left, " || ", right);
        }
    }
}
