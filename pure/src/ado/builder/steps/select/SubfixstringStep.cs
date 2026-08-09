using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.subfix(...).</summary>
    public sealed class SubfixstringStep : StepBase
    {
        public override int Id { get { return 65583; } }
        public override StepKind Kind { get { return StepKind.SelectMisc; } }

        private readonly string _SQLString;

        public SubfixstringStep(string SQLString)
        {
            _SQLString = SQLString;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_SQLString);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.subfix(_SQLString);
    }
}
