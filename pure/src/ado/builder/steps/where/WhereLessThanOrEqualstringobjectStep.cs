namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLessThanOrEqual(...).</summary>
    public sealed class WhereLessThanOrEqualstringobjectStep : WhereKeyCompareStep
    {
        public override int Id { get { return 196721; } }
        protected override string Op { get { return "<="; } }

        public WhereLessThanOrEqualstringobjectStep(string key, object val) : base(key, val) { }
    }
}
