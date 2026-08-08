using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.orderby(...).</summary>
    public sealed class OrderbystringStep : IStep
    {
        private readonly string _orderByPart;

        public OrderbystringStep(string orderByPart)
        {
            _orderByPart = orderByPart;
        }

        public void Apply(StepBuilder builder) => builder.orderby(_orderByPart);
    }
}
