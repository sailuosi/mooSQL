using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setUJsonValue(...)。仅 UPDATE 的 JSON 路径赋值。</summary>
    public sealed class SetUJsonValuestringstringobjectStep : StepBase, IStaticSlotStep
    {
        public override int Id { get { return 262211; } }
        public override StepKind Kind { get { return StepKind.Set; } }

        private readonly string _key;
        private readonly string _path;
        private readonly object _val;

        public SetUJsonValuestringstringobjectStep(string key, string path, object val)
        {
            _key = key;
            _path = path;
            _val = val;
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
            var writes = _val != null && _val != DBNull.Value;
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
                hc.Add(_path);
                return;
            }
            var emit = PassesParaRule(paraRule, _val);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
            hc.Add(_path);
        }

        public override void Apply(SQLBuilder builder)
        {
            if (StaticSlotId != null && !string.IsNullOrEmpty(StaticSlotName))
            {
                builder.Inner.setWithSlot(_key, _val, StaticSlotName, true, null, true, false, DataFam.Undefined, _path);
                return;
            }
            builder.Inner.setUJsonValue(_key, _path, _val);
        }
    }
}
