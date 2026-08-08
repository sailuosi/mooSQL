using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.subfix(...).</summary>
    public sealed class SubfixstringStep : IStep
    {
        private readonly string _SQLString;

        public SubfixstringStep(string SQLString)
        {
            _SQLString = SQLString;
        }

        public void Apply(StepBuilder builder) => builder.subfix(_SQLString);
    }
}
