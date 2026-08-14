namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.clearSelect()"/>。</summary>
    public sealed class ClearSelectStep : StepBase
    {
        public override int Id { get { return 458771; } }
        public override StepKind Kind { get { return StepKind.ClearSelect; } }
                public static readonly ClearSelectStep Instance = new ClearSelectStep();
        private ClearSelectStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.clearSelect();
    }
}
