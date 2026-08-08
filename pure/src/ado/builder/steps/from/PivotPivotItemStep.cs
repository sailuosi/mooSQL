using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.pivot(...).</summary>
    public sealed class PivotPivotItemStep : IStep
    {
        private readonly PivotItem _SQLString;

        public PivotPivotItemStep(PivotItem SQLString)
        {
            _SQLString = SQLString;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.pivot(_SQLString);
    }
}
