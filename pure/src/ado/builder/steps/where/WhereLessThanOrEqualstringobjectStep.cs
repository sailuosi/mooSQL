using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLessThanOrEqual(...).</summary>
    public sealed class WhereLessThanOrEqualstringobjectStep : StepBase {
        public override int Id { get { return 196721; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly object _val;

        public WhereLessThanOrEqualstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereLessThanOrEqual(_key, _val);
    }
}
