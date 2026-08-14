using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotLikeLeftOrNull(...).</summary>
    public sealed class WhereNotLikeLeftOrNullstringstringStep : StepBase {
        public override int Id { get { return 196734; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly string _val;

        public WhereNotLikeLeftOrNullstringstringStep(string key, string val)
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
                public override void Apply(SQLBuilder builder) => builder.Inner.whereNotLikeLeftOrNull(_key, _val);
    }
}
