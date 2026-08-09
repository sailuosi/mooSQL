using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereGreaterThanOrEqual(...).</summary>
    public sealed class WhereGreaterThanOrEqualstringobjectStep : StepBase
    {
        public override int Id { get { return 196705; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly object _val;

        public WhereGreaterThanOrEqualstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereGreaterThanOrEqual(_key, _val);
    }
}
