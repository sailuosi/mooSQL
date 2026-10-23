namespace mooSQL.data
{
    /// <summary>达梦 SQL 函数：与 Oracle 系一致（LENGTH/SUBSTR/INSTR/SYSDATE/EXTRACT/||）。</summary>
    public class DMSQLFunction : SooSQLFunction
    {
 /// <summary>
 /// Builds a length expression.
 /// </summary>
 
        public override string Len(string FieldSQL)
            => "LENGTH(" + FieldSQL + ")";
/// <summary>
/// Builds a substring expression.
/// </summary>
 

        public override string SubStr(string FieldSQL, int start, int length)
            => string.Concat("SUBSTR(", FieldSQL, ",", start.ToString(), ",", length.ToString(), ")");
/// <summary>
/// Builds a character-index expression.
/// </summary>
 

        public override string CharIndex(string subString, string str)
            => string.Concat("INSTR(", str, ",", subString, ")");
/// <summary>
/// Builds a current-timestamp expression.
/// </summary>
 

        public override string Now()
            => "SYSDATE";
/// <summary>
/// Builds a year-part expression.
/// </summary>
 

        public override string Year(string FieldSQL)
            => string.Concat("EXTRACT(YEAR FROM", FieldSQL, ")");
/// <summary>
/// Builds a month-part expression.
/// </summary>
 

        public override string Month(string FieldSQL)
            => string.Concat("EXTRACT(MONTH  FROM", FieldSQL, ")");
/// <summary>
/// Builds a day-part expression.
/// </summary>
 

        public override string Day(string FieldSQL)
            => string.Concat("EXTRACT(DAY  FROM", FieldSQL, ")");
/// <summary>
/// Builds a string-concatenation expression.
/// </summary>
 

        public override string Concat(string left, string right)
            => string.Concat(left, " || ", right);
    }
}
