namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.set(key, value, maxLength)。</summary>
    public sealed class SetstringstringintStep : StepBase, IStaticSlotStep
    {
        public override int Id { get { return 262200; } }
        public override StepKind Kind { get { return StepKind.Set; } }

        private readonly string _key;
        private readonly string _value;
        private readonly int _maxLength;

        public SetstringstringintStep(string key, string value, int maxLength)
        {
            _key = key;
            _value = value;
            _maxLength = maxLength;
        }

        private string EffectiveValue
        {
            get
            {
                var v = _value;
                if (v != null && v.Length > _maxLength)
                    v = v.Substring(0, _maxLength);
                return v;
            }
        }

        public int? StaticSlotId { get; private set; }

        public string StaticSlotName { get; private set; }

        public object StaticSlotValue { get { return EffectiveValue; } }

        internal void TryAssignStaticSlot(
            string paraRule,
            ref bool opened,
            ref int nextStaticSlot,
            string paraSeed,
            string groupKey)
        {
            var v = EffectiveValue;
            var writes = v != null;
            StaticSlotId = TryAllocStaticSlotId(paraRule, v, writes, ref opened, ref nextStaticSlot);
            if (StaticSlotId != null)
                StaticSlotName = StaticSlotMarks.FormatSetName(paraSeed, groupKey, StaticSlotId.Value);
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
                hc.Add(_maxLength);
                return;
            }
            var emit = PassesParaRule(paraRule, _value);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
            hc.Add(_maxLength);
        }

        public override void Apply(SQLBuilder builder)
        {
            var v = EffectiveValue;
            if (StaticSlotId != null && !string.IsNullOrEmpty(StaticSlotName))
            {
                builder.Inner.setWithSlot(_key, v, StaticSlotName);
                return;
            }
            builder.Inner.set(_key, v, _maxLength);
        }
    }
}
