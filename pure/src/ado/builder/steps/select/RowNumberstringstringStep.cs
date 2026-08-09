using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rowNumber(...).</summary>
    public sealed class RowNumberstringstringStep : StepBase
    {
        public override int Id { get { return 65573; } }
        public override StepKind Kind { get { return StepKind.RowNumber; } }

        private readonly string _orderPart;
        private readonly string _asName;

        public RowNumberstringstringStep(string orderPart, string asName)
        {
            _orderPart = orderPart;
            _asName = asName;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_orderPart);
            hc.Add(_asName);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.rowNumber(_orderPart, _asName);
    }
}
