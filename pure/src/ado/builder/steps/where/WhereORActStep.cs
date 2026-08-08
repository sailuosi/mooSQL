using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereOR(...).</summary>
    public sealed class WhereORActStep : IStep
    {
        private readonly Action<SQLBuilder> _whereBuilder;

        public WhereORActStep(Action<SQLBuilder> whereBuilder)
        {
            _whereBuilder = whereBuilder;
        }

        public void Apply(StepBuilder builder) => builder.whereOR(_whereBuilder);
    }
}
