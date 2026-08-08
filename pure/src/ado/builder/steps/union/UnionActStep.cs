using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.union(...).</summary>
    public sealed class UnionActStep : IStep
    {
        private readonly Action<SQLBuilder> _doUnion;

        public UnionActStep(Action<SQLBuilder> doUnion)
        {
            _doUnion = doUnion;
        }

        public void Apply(StepBuilder builder) => builder.union(_doUnion);
    }
}
