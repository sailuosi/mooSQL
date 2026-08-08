using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.take(...).</summary>
    public sealed class TakeintStep : IStep
    {
        private readonly int _skip;

        public TakeintStep(int skip)
        {
            _skip = skip;
        }

        public void Apply(StepBuilder builder) => builder.take(_skip);
    }
}
