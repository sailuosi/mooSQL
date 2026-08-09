namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.where(string, object, string, bool)"/>。</summary>
    public sealed class WhereKeyValOpParamedStep : StepBase {
        public override int Id { get { return 196719; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly object _val;
        private readonly string _op;
        private readonly bool _paramed;

        public WhereKeyValOpParamedStep(string key, object val, string op, bool paramed)
        {
            _key = key;
            _val = val;
            _op = op;
            _paramed = paramed;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                hc.Add(_op);
                hc.Add(_paramed);
                return;
            }
            var emit = PassesParaRule(paraRule, _val);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
            hc.Add(_op);
            hc.Add(_paramed);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.where(_key, _val, _op, _paramed);
    }
}
