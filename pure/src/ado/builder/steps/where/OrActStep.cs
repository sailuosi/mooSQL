using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.or(...).</summary>
    public sealed class OrActStep : IStep
    {
        private readonly Action<SQLBuilder> _doSomeWhere;

        public OrActStep(Action<SQLBuilder> doSomeWhere)
        {
            _doSomeWhere = doSomeWhere;
        }

        public void Apply(StepBuilder builder) => builder.or(_doSomeWhere);
    }
}
