using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setU(...).</summary>
    public sealed class SetUstringobjectStep : StepBase
    {
        public override int Id { get { return 262204; } }
        public override StepKind Kind { get { return StepKind.Set; } }

        private readonly string _key;
        private readonly object _val;

        public SetUstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
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
                public override void Apply(SQLBuilder builder) => builder.Inner.setU(_key, _val);
    }
}
