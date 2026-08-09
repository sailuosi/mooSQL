using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotLikeLeft(...).</summary>
    public sealed class WhereNotLikeLeftstringstringStep : StepBase {
        public override int Id { get { return 196736; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly string _val;

        public WhereNotLikeLeftstringstringStep(string key, string val)
        {
            _key = key;
            _val = val;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereNotLikeLeft(_key, _val);
    }
}
