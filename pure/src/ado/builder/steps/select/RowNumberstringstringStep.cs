using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rowNumber(...).</summary>
    public sealed class RowNumberstringstringStep : IStep
    {
        private readonly string _orderPart;
        private readonly string _asName;

        public RowNumberstringstringStep(string orderPart, string asName)
        {
            _orderPart = orderPart;
            _asName = asName;
        }

        public void Apply(StepBuilder builder) => builder.rowNumber(_orderPart, _asName);
    }
}
