using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.selectWith(...).</summary>
    public sealed class SelectWithstringStep : IStep
    {
        private readonly string _queryOther;

        public SelectWithstringStep(string queryOther)
        {
            _queryOther = queryOther;
        }

        public void Apply(StepBuilder builder) => builder.selectWith(_queryOther);
    }
}
