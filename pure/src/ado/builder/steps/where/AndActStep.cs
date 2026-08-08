using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.and(...).</summary>
    public sealed class AndActStep : IStep
    {
        private readonly Action<SQLBuilder> _doSomeWhere;

        public AndActStep(Action<SQLBuilder> doSomeWhere)
        {
            _doSomeWhere = doSomeWhere;
        }

        public void Apply(StepBuilder builder) => builder.and(_doSomeWhere);
    }
}
