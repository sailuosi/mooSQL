namespace mooSQL.data
{
    /// <summary>达梦 SQL 函数：与 Oracle 系一致（LENGTH/SUBSTR/INSTR/SYSDATE/EXTRACT/||）。</summary>
    public class DMSQLFunction : SooSQLFunction
    {
        public override string Len(string FieldSQL)
            => "LENGTH(" + FieldSQL + ")";

        public override string SubStr(string FieldSQL, int start, int length)
            => string.Concat("SUBSTR(", FieldSQL, ",", start.ToString(), ",", length.ToString(), ")");

        public override string CharIndex(string subString, string str)
            => string.Concat("INSTR(", str, ",", subString, ")");

        public override string Now()
            => "SYSDATE";

        public override string Year(string FieldSQL)
            => string.Concat("EXTRACT(YEAR FROM", FieldSQL, ")");

        public override string Month(string FieldSQL)
            => string.Concat("EXTRACT(MONTH  FROM", FieldSQL, ")");

        public override string Day(string FieldSQL)
            => string.Concat("EXTRACT(DAY  FROM", FieldSQL, ")");

        public override string Concat(string left, string right)
            => string.Concat(left, " || ", right);
    }
}
