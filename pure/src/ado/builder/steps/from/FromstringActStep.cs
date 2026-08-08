using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.from(...).</summary>
    public sealed class FromstringActStep : IStep
    {
        private readonly string _asName;
        private readonly Action<SQLBuilder> _childFromPart;

        public FromstringActStep(string asName, Action<SQLBuilder> childFromPart)
        {
            _asName = asName;
            _childFromPart = childFromPart;
        }

        public void Apply(StepBuilder builder) => builder.from(_asName, _childFromPart);
    }
}
