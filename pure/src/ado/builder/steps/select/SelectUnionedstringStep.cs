using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.selectUnioned(...).</summary>
    public sealed class SelectUnionedstringStep : StepBase
    {
        public override int Id { get { return 65578; } }
        public override StepKind Kind { get { return StepKind.Select; } }

        private readonly string _columns;

        public SelectUnionedstringStep(string columns)
        {
            _columns = columns;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_columns);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.selectUnioned(_columns);
    }
}
