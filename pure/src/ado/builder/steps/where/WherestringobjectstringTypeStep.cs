using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(...).</summary>
    public sealed class WherestringobjectstringTypeStep : StepBase {
        public override int Id { get { return 196741; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly object _val;
        private readonly string _op;
        private readonly Type _t;

        public WherestringobjectstringTypeStep(string key, object val, string op, Type t)
        {
            _key = key;
            _val = val;
            _op = op;
            _t = t;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
            hc.Add(_op);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.where(_key, _val, _op, _t);
    }
}
