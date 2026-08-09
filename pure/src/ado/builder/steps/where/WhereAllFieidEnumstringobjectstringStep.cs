using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereAllFieid(...).</summary>
    public sealed class WhereAllFieidEnumstringobjectstringStep : StepBase
    {
        public override int Id { get { return 196692; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly IEnumerable<string> _fields;
        private readonly object _value;
        private readonly string _op;

        public WhereAllFieidEnumstringobjectstringStep(IEnumerable<string> fields, object value, string op)
        {
            _fields = fields;
            _value = value;
            _op = op;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_op);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereAllFieid(_fields, _value, _op);
    }
}
