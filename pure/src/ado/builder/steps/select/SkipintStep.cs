using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.skip(...).</summary>
    public sealed class SkipintStep : IStep
    {
        private readonly int _skip;

        public SkipintStep(int skip)
        {
            _skip = skip;
        }

        public void Apply(StepBuilder builder) => builder.skip(_skip);
    }
}
