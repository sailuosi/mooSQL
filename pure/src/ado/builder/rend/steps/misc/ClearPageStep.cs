namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.clearPage()"/>。</summary>
    public sealed class ClearPageStep : StepBase
    {
        public override int Id { get { return 458770; } }
        public override StepKind Kind { get { return StepKind.ClearPage; } }
                public static readonly ClearPageStep Instance = new ClearPageStep();
        private ClearPageStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.clearPage();
    }
}
