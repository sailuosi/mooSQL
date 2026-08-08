using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.unpivot(...).</summary>
    public sealed class UnpivotUnpivotItemStep : IStep
    {
        private readonly UnpivotItem _SQLString;

        public UnpivotUnpivotItemStep(UnpivotItem SQLString)
        {
            _SQLString = SQLString;
        }

        public void Apply(StepBuilder builder) => builder.unpivot(_SQLString);
    }
}
