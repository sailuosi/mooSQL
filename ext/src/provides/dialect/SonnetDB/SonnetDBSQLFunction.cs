#if NET10_0_OR_GREATER
namespace mooSQL.data
{
    /// <inheritdoc/>
    public class SonnetDBSQLFunction : SooSQLFunction
    {
        /// <inheritdoc/>
        public override string Len(string FieldSQL) => "LENGTH(" + FieldSQL + ")";

        /// <inheritdoc/>
        public override string SubStr(string FieldSQL, int start, int length)
            => string.Concat("SUBSTRING(", FieldSQL, ", ", start.ToString(), ", ", length.ToString(), ")");

        /// <inheritdoc/>
        public override string CharIndex(string subString, string str)
            => string.Concat("INSTR(", str, ",", subString, ")");

        /// <inheritdoc/>
        public override string Now() => "CURRENT_DATETIME()";

        /// <inheritdoc/>
        public override string Year(string FieldSQL)
            => string.Concat("YEAR(", FieldSQL, ")");

        /// <inheritdoc/>
        public override string Month(string FieldSQL)
            => string.Concat("MONTH(", FieldSQL, ")");

        /// <inheritdoc/>
        public override string Day(string FieldSQL)
            => string.Concat("DAY(", FieldSQL, ")");
    }
}
#endif
