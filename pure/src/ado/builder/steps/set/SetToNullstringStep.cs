using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setToNull(...).</summary>
    public sealed class SetToNullstringStep : IStep
    {
        private readonly string _fieldName;

        public SetToNullstringStep(string fieldName)
        {
            _fieldName = fieldName;
        }

        public void Apply(StepBuilder builder) => builder.setToNull(_fieldName);
    }
}
