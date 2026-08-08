using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.selectSummary(...).</summary>
    public sealed class SelectSummarystringStep : IStep
    {
        private readonly string _queryOther;

        public SelectSummarystringStep(string queryOther)
        {
            _queryOther = queryOther;
        }

        public void Apply(StepBuilder builder) => builder.selectSummary(_queryOther);
    }
}
