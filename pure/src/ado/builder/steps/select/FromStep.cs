namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.from(string)"/>。</summary>
    public sealed class FromStep : StepBase
    {
        public override int Id { get { return 65565; } }
        public override StepKind Kind { get { return StepKind.From; } }

        private readonly string _fromPart;
        public FromStep(string fromPart) => _fromPart = fromPart;
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_fromPart);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.from(_fromPart);
    }
}
