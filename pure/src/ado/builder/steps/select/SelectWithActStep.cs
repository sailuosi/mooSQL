using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.selectWith(...).</summary>
    public sealed class SelectWithActStep : IStep
    {
        private readonly Action<SQLBuilder> _queryOther;

        public SelectWithActStep(Action<SQLBuilder> queryOther)
        {
            _queryOther = queryOther;
        }

        public void Apply(StepBuilder builder) => builder.selectWith(_queryOther);
    }
}
