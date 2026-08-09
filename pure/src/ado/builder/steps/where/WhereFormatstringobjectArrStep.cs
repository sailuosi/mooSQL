using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereFormat(...).</summary>
    public sealed class WhereFormatstringobjectArrStep : StepBase
    {
        public override int Id { get { return 196699; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _template;
        private readonly object[] _values;

        public WhereFormatstringobjectArrStep(string template, params object[] values)
        {
            _template = template;
            _values = values;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_template);
                return;
            }
            var emit = true;
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_template);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereFormat(_template, _values);
    }
}
