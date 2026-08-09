using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.unpivot(...).</summary>
    public sealed class UnpivotUnpivotItemStep : StepBase
    {
        public override int Id { get { return 131083; } }
        public override StepKind Kind { get { return StepKind.PivotUnpivot; } }

        private readonly UnpivotItem _SQLString;

        public UnpivotUnpivotItemStep(UnpivotItem SQLString)
        {
            _SQLString = SQLString;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_SQLString);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.unpivot(_SQLString);
    }
}
