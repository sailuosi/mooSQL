namespace mooSQL.data
{
    /// <summary>对应 <see cref="SQLBuilder.where(string, object, string, bool)"/>。paramed 时走 StaticSlot。</summary>
    public sealed class WhereKeyValOpParamedStep : StepBase, IStaticSlotStep
    {
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

        /// <inheritdoc />
        public int? StaticSlotId { get; private set; }

        /// <inheritdoc />
        public string StaticSlotName { get; private set; }

        /// <inheritdoc />
        public object StaticSlotValue { get { return _val; } }

        internal void TryAssignStaticSlot(
            string paraRule,
            ref bool opened,
            ref int nextStaticSlot,
            string paraSeed,
            string groupParamPrefix)
        {
            StaticSlotId = TryAllocStaticSlotId(paraRule, _val, _paramed, ref opened, ref nextStaticSlot);
            if (StaticSlotId != null)
                StaticSlotName = StaticSlotMarks.FormatWhereName(paraSeed, groupParamPrefix, StaticSlotId.Value);
            else
                StaticSlotName = null;
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

        public override void Apply(SQLBuilder builder)
        {
            if (StaticSlotId != null && !string.IsNullOrEmpty(StaticSlotName))
            {
                builder.Inner.whereWithSlot(_key, _val, StaticSlotName, _op);
                return;
            }
            builder.Inner.where(_key, _val, _op, _paramed);
        }
    }
}
