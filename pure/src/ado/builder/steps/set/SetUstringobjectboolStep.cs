namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setU(..., paramed)。</summary>
    public sealed class SetUstringobjectboolStep : StepBase, IStaticSlotStep
    {
        public override int Id { get { return 262203; } }
        public override StepKind Kind { get { return StepKind.Set; } }

        private readonly string _key;
        private readonly object _val;
        private readonly bool _paramed;

        public SetUstringobjectboolStep(string key, object val, bool paramed)
        {
            _key = key;
            _val = val;
            _paramed = paramed;
        }

        public int? StaticSlotId { get; private set; }

        public object StaticSlotValue { get { return _val; } }

        internal void TryAssignStaticSlot(string paraRule, ref bool opened, ref int nextStaticSlot)
        {
            var writes = _paramed && _val != null && _val != System.DBNull.Value;
            StaticSlotId = TryAllocStaticSlotId(paraRule, _val, writes, ref opened, ref nextStaticSlot);
        }

        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                hc.Add(_paramed);
                return;
            }
            var emit = PassesParaRule(paraRule, _val);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
            hc.Add(_paramed);
        }

        public override void Apply(SQLBuilder builder)
        {
            if (StaticSlotId != null)
            {
                builder.Inner.setWithSlot(_key, _val, StaticSlotId.Value, _paramed, null, true, false);
                return;
            }
            builder.Inner.setU(_key, _val, _paramed);
        }
    }
}
