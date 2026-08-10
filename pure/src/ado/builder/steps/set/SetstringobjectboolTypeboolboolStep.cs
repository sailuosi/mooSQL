using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.set(...)。paramed 时支持编排期 StaticSlotId。</summary>
    public sealed class SetstringobjectboolTypeboolboolStep : StepBase, IStaticSlotStep
    {
        public override int Id { get { return 262199; } }
        public override StepKind Kind { get { return StepKind.Set; } }

        private readonly string _key;
        private readonly object _val;
        private readonly bool _paramed;
        private readonly Type _type;
        private readonly bool _updatable;
        private readonly bool _insertable;

        public SetstringobjectboolTypeboolboolStep(string key, object val, bool paramed, Type type, bool updatable, bool insertable)
        {
            _key = key;
            _val = val;
            _paramed = paramed;
            _type = type;
            _updatable = updatable;
            _insertable = insertable;
        }

        public int? StaticSlotId { get; private set; }

        public string StaticSlotName { get; private set; }

        public object StaticSlotValue { get { return _val; } }

        internal void TryAssignStaticSlot(
            string paraRule,
            ref bool opened,
            ref int nextStaticSlot,
            string paraSeed,
            string groupKey)
        {
            var writes = _paramed && _val != null && _val != DBNull.Value;
            StaticSlotId = TryAllocStaticSlotId(paraRule, _val, writes, ref opened, ref nextStaticSlot);
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
                hc.Add(_paramed);
                hc.Add(_updatable);
                hc.Add(_insertable);
                return;
            }
            var emit = PassesParaRule(paraRule, _val);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
            hc.Add(_paramed);
            hc.Add(_updatable);
            hc.Add(_insertable);
        }

        public override void Apply(SQLBuilder builder)
        {
            if (StaticSlotId != null && !string.IsNullOrEmpty(StaticSlotName))
            {
                builder.Inner.setWithSlot(_key, _val, StaticSlotName, _paramed, _type, _updatable, _insertable);
                return;
            }
            builder.Inner.set(_key, _val, _paramed, _type, _updatable, _insertable);
        }
    }
}
