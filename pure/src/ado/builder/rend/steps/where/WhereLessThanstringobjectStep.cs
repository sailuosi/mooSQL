namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLessThan(...).</summary>
    public sealed class WhereLessThanstringobjectStep : WhereKeyCompareStep
    {
        public override int Id { get { return 196722; } }
        protected override string Op { get { return "<"; } }

        public WhereLessThanstringobjectStep(string key, object val) : base(key, val) { }
    }
}
