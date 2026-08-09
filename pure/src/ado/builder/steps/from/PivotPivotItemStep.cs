using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.pivot(...).</summary>
    public sealed class PivotPivotItemStep : StepBase
    {
        public override int Id { get { return 131080; } }
        public override StepKind Kind { get { return StepKind.PivotUnpivot; } }

        private readonly PivotItem _SQLString;

        public PivotPivotItemStep(PivotItem SQLString)
        {
            _SQLString = SQLString;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_SQLString);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.pivot(_SQLString);
    }
}
