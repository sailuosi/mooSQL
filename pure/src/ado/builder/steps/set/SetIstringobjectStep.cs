namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setI(...)。</summary>
    public sealed class SetIstringobjectStep : StepBase, IStaticSlotStep
    {
        public override int Id { get { return 262198; } }
        public override StepKind Kind { get { return StepKind.Set; } }

        private readonly string _key;
        private readonly object _val;

        public SetIstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public int? StaticSlotId { get; private set; }

        public object StaticSlotValue { get { return _val; } }

        internal void TryAssignStaticSlot(string paraRule, ref bool opened, ref int nextStaticSlot)
        {
            var writes = _val != null && _val != System.DBNull.Value;
            StaticSlotId = TryAllocStaticSlotId(paraRule, _val, writes, ref opened, ref nextStaticSlot);
        }

        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                return;
            }
            var emit = PassesParaRule(paraRule, _val);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }

        public override void Apply(SQLBuilder builder)
        {
            if (StaticSlotId != null)
            {
                builder.Inner.setWithSlot(_key, _val, StaticSlotId.Value, true, null, false, true);
                return;
            }
            builder.Inner.setI(_key, _val);
        }
    }
}
