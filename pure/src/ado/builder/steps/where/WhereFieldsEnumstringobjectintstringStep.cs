using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereFields(...).</summary>
    public sealed class WhereFieldsEnumstringobjectintstringStep : StepBase
    {
        public override int Id { get { return 196698; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly IEnumerable<string> _fields;
        private readonly object _value;
        private readonly int _SinkMode;
        private readonly string _op;

        public WhereFieldsEnumstringobjectintstringStep(IEnumerable<string> fields, object value, int SinkMode, string op)
        {
            _fields = fields;
            _value = value;
            _SinkMode = SinkMode;
            _op = op;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_SinkMode);
            hc.Add(_op);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereFields(_fields, _value, _SinkMode, _op);
    }
}
