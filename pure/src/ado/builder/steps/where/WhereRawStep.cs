namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.where(string)"/>。</summary>
    public sealed class WhereRawStep : StepBase {
        public override int Id { get { return 196739; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        public WhereRawStep(string key) => _key = key;
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                return;
            }
            var emit = !string.IsNullOrWhiteSpace(_key);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.where(_key);
    }
}
