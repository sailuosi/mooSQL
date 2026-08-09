namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotEqual(...).</summary>
    public sealed class WhereNotEqualstringobjectStep : WhereKeyCompareStep
    {
        public override int Id { get { return 196731; } }
        protected override string Op { get { return "<>"; } }

        public WhereNotEqualstringobjectStep(string key, object val) : base(key, val) { }
    }
}
