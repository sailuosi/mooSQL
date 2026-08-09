using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIf(...).</summary>
    public sealed class WhereIfboolNstringobjectstringStep : StepBase
    {
        public override int Id { get { return 196708; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly bool? _isTrue;
        private readonly string _key;
        private readonly object _val;
        private readonly string _op;

        public WhereIfboolNstringobjectstringStep(bool? isTrue, string key, object val, string op)
        {
            _isTrue = isTrue;
            _key = key;
            _val = val;
            _op = op;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
            hc.Add(_op);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereIf(_isTrue, _key, _val, _op);
    }
}
