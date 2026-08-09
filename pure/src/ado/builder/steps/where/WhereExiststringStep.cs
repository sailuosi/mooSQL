using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereExist(...).</summary>
    public sealed class WhereExiststringStep : StepBase
    {
        public override int Id { get { return 196697; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _value;

        public WhereExiststringStep(string value)
        {
            _value = value;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                return;
            }
            var emit = PassesParaRule(paraRule, _value);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.whereExist(_value);
    }
}
