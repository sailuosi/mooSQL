namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.distinct()"/>。</summary>
    public sealed class DistinctStep : StepBase
    {
        public override int Id { get { return 65564; } }
        public override StepKind Kind { get { return StepKind.Distinct; } }

        public static readonly DistinctStep Instance = new DistinctStep();
        private DistinctStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.distinct();
    }
}
