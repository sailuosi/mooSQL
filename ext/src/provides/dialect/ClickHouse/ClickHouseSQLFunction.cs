#if NET6_0_OR_GREATER
namespace mooSQL.data
{
    /// <inheritdoc/>
    public class ClickHouseSQLFunction : SooSQLFunction
    {
        /// <inheritdoc/>
        public override string Len(string FieldSQL) => "length(" + FieldSQL + ")";

        /// <inheritdoc/>
        public override string SubStr(string FieldSQL, int start, int length)
            => string.Concat("substring(", FieldSQL, ", ", start.ToString(), ", ", length.ToString(), ")");

        /// <inheritdoc/>
        public override string CharIndex(string subString, string str)
            => string.Concat("position(", str, ", ", subString, ")");

        /// <inheritdoc/>
        public override string Now() => "now64(3)";

        /// <inheritdoc/>
        public override string Year(string FieldSQL)
            => string.Concat("toYear(toDateTime64(", FieldSQL, ", 3))");

        /// <inheritdoc/>
        public override string Month(string FieldSQL)
            => string.Concat("toMonth(toDateTime64(", FieldSQL, ", 3))");

        /// <inheritdoc/>
        public override string Day(string FieldSQL)
            => string.Concat("toDayOfMonth(toDateTime64(", FieldSQL, ", 3))");
    }
}
#endif
