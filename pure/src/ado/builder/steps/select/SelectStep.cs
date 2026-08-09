namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.select(string)"/>。</summary>
    public sealed class SelectStep : StepBase
    {
        public override int Id { get { return 65576; } }
        public override StepKind Kind { get { return StepKind.Select; } }

        private readonly string _columns;
        public SelectStep(string columns) => _columns = columns;
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_columns);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.select(_columns);
    }
}
