using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(...).</summary>
    public sealed class WhereActStep : IStep
    {
        private readonly Action<SQLBuilder> _whereBuilder;

        public WhereActStep(Action<SQLBuilder> whereBuilder)
        {
            _whereBuilder = whereBuilder;
        }

        public void Apply(StepBuilder builder) => builder.where(_whereBuilder);
    }
}
