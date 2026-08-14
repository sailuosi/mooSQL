namespace mooSQL.data
{
    /// <summary>
    /// 单值比较 where（&gt; / &lt; / &gt;= / &lt;= / &lt;&gt; 等）的 StaticSlot 公共实现。
    /// </summary>
    public abstract class WhereKeyCompareStep : StepBase, IStaticSlotStep
    {
        public sealed override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly object _val;

        protected WhereKeyCompareStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        protected string Key { get { return _key; } }
        protected object Val { get { return _val; } }

        /// <summary>比较符（如 &gt;、&lt;&gt;）。</summary>
        protected abstract string Op { get; }

        public int? StaticSlotId { get; private set; }

        public string StaticSlotName { get; private set; }

        public object StaticSlotValue { get { return _val; } }

        internal void TryAssignStaticSlot(
            string paraRule,
            ref bool opened,
            ref int nextStaticSlot,
            string paraSeed,
            string groupParamPrefix)
        {
            StaticSlotId = TryAllocStaticSlotId(paraRule, _val, true, ref opened, ref nextStaticSlot);
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
                return;
            }
            var emit = PassesParaRule(paraRule, _val);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }

        public override void Apply(SQLBuilder builder)
        {
            if (StaticSlotId != null && !string.IsNullOrEmpty(StaticSlotName))
            {
                builder.Inner.whereWithSlot(_key, _val, StaticSlotName, Op);
                return;
            }
            builder.Inner.where(_key, _val, Op, true);
        }
    }
}
