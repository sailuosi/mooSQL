namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereGreaterThanOrEqual(...).</summary>
    public sealed class WhereGreaterThanOrEqualstringobjectStep : WhereKeyCompareStep
    {
        public override int Id { get { return 196705; } }
        protected override string Op { get { return ">="; } }

        public WhereGreaterThanOrEqualstringobjectStep(string key, object val) : base(key, val) { }
    }
}
