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
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_orderPart);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.rowNumber(_orderPart);
    }
}
