using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rowNumber(...).</summary>
    public sealed class RowNumberstringStep : IStep
    {
        private readonly string _orderPart;

        public RowNumberstringStep(string orderPart)
        {
            _orderPart = orderPart;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.rowNumber(_orderPart);
    }
}
