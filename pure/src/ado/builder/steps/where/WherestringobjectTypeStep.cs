using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(...).</summary>
    public sealed class WherestringobjectTypeStep : StepBase {
        public override int Id { get { return 196742; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly object _val;
        private readonly Type _t;

        public WherestringobjectTypeStep(string key, object val, Type t)
        {
            _key = key;
            _val = val;
            _t = t;
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
                public override void Apply(SQLBuilder builder) => builder.Inner.where(_key, _val, _t);
    }
}
