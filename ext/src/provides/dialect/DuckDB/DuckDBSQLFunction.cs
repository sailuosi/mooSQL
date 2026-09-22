#if NET6_0_OR_GREATER
namespace mooSQL.data
{
    /// <inheritdoc/>
    public class DuckDBSQLFunction : SooSQLFunction
    {
        /// <inheritdoc/>
        public override string Len(string FieldSQL) => "LENGTH(" + FieldSQL + ")";

        /// <inheritdoc/>
        public override string SubStr(string FieldSQL, int start, int length)
            => string.Concat("SUBSTRING(", FieldSQL, " FROM ", start.ToString(), " FOR ", length.ToString(), ")");

        /// <inheritdoc/>
        public override string CharIndex(string subString, string str)
            => string.Concat("STRPOS(", str, ",", subString, ")");

        /// <inheritdoc/>
        public override string Now() => "CURRENT_TIMESTAMP";

        /// <inheritdoc/>
        public override string Year(string FieldSQL)
            => string.Concat("CAST(date_part('year',", FieldSQL, ") AS INTEGER)");

        /// <inheritdoc/>
        public override string Month(string FieldSQL)
            => string.Concat("CAST(date_part('month',", FieldSQL, ") AS INTEGER)");

        /// <inheritdoc/>
        public override string Day(string FieldSQL)
            => string.Concat("CAST(date_part('day',", FieldSQL, ") AS INTEGER)");
    }
}
#endif
