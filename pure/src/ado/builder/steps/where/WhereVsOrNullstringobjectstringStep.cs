using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereVsOrNull(...).</summary>
    public sealed class WhereVsOrNullstringobjectstringStep : StepBase {
        public override int Id { get { return 196743; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly object _val;
        private readonly string _op;

        public WhereVsOrNullstringobjectstringStep(string key, object val, string op)
        {
            _key = key;
            _val = val;
            _op = op;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
            hc.Add(_op);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereVsOrNull(_key, _val, _op);
    }
}
