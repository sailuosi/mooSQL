namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereGreaterThan(...).</summary>
    public sealed class WhereGreaterThanstringobjectStep : WhereKeyCompareStep
    {
        public override int Id { get { return 196706; } }
        protected override string Op { get { return ">"; } }

        public WhereGreaterThanstringobjectStep(string key, object val) : base(key, val) { }
    }
}
