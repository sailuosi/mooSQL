using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereAnyFieid(...).</summary>
    public sealed class WhereAnyFieidEnumstringobjectstringStep : StepBase
    {
        public override int Id { get { return 196693; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly IEnumerable<string> _fields;
        private readonly object _value;
        private readonly string _op;

        public WhereAnyFieidEnumstringobjectstringStep(IEnumerable<string> fields, object value, string op)
        {
            _fields = fields;
            _value = value;
            _op = op;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_op);
                return;
            }
            var emit = PassesParaRule(paraRule, _value);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_op);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereAnyFieid(_fields, _value, _op);
    }
}
