using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.orderby(...).</summary>
    public sealed class OrderbystringStep : StepBase
    {
        public override int Id { get { return 65569; } }
        public override StepKind Kind { get { return StepKind.OrderBy; } }

        private readonly string _orderByPart;

        public OrderbystringStep(string orderByPart)
        {
            _orderByPart = orderByPart;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_orderByPart);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.orderby(_orderByPart);
    }
}
