using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rowNumber(...).</summary>
    public sealed class RowNumberstringStep : StepBase
    {
        public override int Id { get { return 65572; } }
        public override StepKind Kind { get { return StepKind.RowNumber; } }

        private readonly string _orderPart;

        public RowNumberstringStep(string orderPart)
        {
            _orderPart = orderPart;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_orderPart);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.rowNumber(_orderPart);
    }
}
