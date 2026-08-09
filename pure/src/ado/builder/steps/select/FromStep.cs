namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.from(string)"/>。</summary>
    public sealed class FromStep : StepBase
    {
        public override int Id { get { return 65565; } }
        public override StepKind Kind { get { return StepKind.From; } }

        private readonly string _fromPart;
        public FromStep(string fromPart) => _fromPart = fromPart;
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_fromPart);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.from(_fromPart);
    }
}
