using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereAnyFieldIs(...).</summary>
    public sealed class WhereAnyFieldIsobjectstringArrStep : StepBase
    {
        public override int Id { get { return 196694; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly object _value;
        private readonly string[] _fields;

        public WhereAnyFieldIsobjectstringArrStep(object value, params string[] fields)
        {
            _value = value;
            _fields = fields;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                return;
            }
            var emit = PassesParaRule(paraRule, _value);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.whereAnyFieldIs(_value, _fields);
    }
}
