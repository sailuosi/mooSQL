namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.where(string)"/>。</summary>
    public sealed class WhereRawStep : StepBase {
        public override int Id { get { return 196739; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        public WhereRawStep(string key) => _key = key;
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.where(_key);
    }
}
