using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.unionAll(...).</summary>
    public sealed class UnionAllboolstringStep : StepBase
    {
        public override int Id { get { return 327750; } }
        public override StepKind Kind { get { return StepKind.Union; } }

        private readonly bool _wrapSelect;
        private readonly string _wrapAsName;

        public UnionAllboolstringStep(bool wrapSelect, string wrapAsName)
        {
            _wrapSelect = wrapSelect;
            _wrapAsName = wrapAsName;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_wrapSelect);
            hc.Add(_wrapAsName);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.unionAll(_wrapSelect, _wrapAsName);
    }
}
