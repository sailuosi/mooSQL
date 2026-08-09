namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.select(string)"/>。</summary>
    public sealed class SelectStep : StepBase
    {
        public override int Id { get { return 65576; } }
        public override StepKind Kind { get { return StepKind.Select; } }

        private readonly string _columns;
        public SelectStep(string columns) => _columns = columns;
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_columns);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.select(_columns);
    }
}
