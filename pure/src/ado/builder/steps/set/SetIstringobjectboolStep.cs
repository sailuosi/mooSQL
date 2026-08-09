using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setI(...).</summary>
    public sealed class SetIstringobjectboolStep : StepBase
    {
        public override int Id { get { return 262197; } }
        public override StepKind Kind { get { return StepKind.Set; } }

        private readonly string _key;
        private readonly object _val;
        private readonly bool _paramed;

        public SetIstringobjectboolStep(string key, object val, bool paramed)
        {
            _key = key;
            _val = val;
            _paramed = paramed;
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
                public override void Apply(SQLBuilder builder) => builder.Inner.setI(_key, _val, _paramed);
    }
}
