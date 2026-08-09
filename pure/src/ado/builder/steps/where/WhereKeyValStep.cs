namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(key, val)。支持编排期 StaticSlotId（方案 C 试点）。</summary>
    public sealed class WhereKeyValStep : StepBase
    {
        public override int Id { get { return 196720; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly object _val;

        /// <summary>本步 where 列名（热路径收值/校验用）。</summary>
        internal string Key { get { return _key; } }

        /// <summary>本步逻辑值（热路径重绑用；不进缓存）。</summary>
        internal object Value { get { return _val; } }

        /// <summary>编排期分配的静态槽；null 表示本步不写静态参（或未走槽位制）。</summary>
        public int? StaticSlotId { get; private set; }

        public WhereKeyValStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        /// <summary>
        /// 与 ContributeHash 的 Opened 消费 + paraRule 对齐后占槽。
        /// Opened=false 时消费门控（置回 true）且不占槽。
        /// </summary>
        internal void TryAssignStaticSlot(string paraRule, ref bool opened, ref int nextStaticSlot)
        {
            if (!opened)
            {
                opened = true;
                return;
            }
            if (!PassesParaRule(paraRule, _val)) return;
            StaticSlotId = nextStaticSlot;
            nextStaticSlot++;
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
                builder.Inner.whereWithSlot(_key, _val, StaticSlotId.Value);
                return;
            }
            builder.Inner.where(_key, _val);
        }
    }
}
