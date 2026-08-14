namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.clearWhere()"/>。</summary>
    public sealed class ClearWhereStep : StepBase
    {
        public override int Id { get { return 458772; } }
        public override StepKind Kind { get { return StepKind.ClearWhere; } }
                public static readonly ClearWhereStep Instance = new ClearWhereStep();
        private ClearWhereStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.clearWhere();
    }
}
