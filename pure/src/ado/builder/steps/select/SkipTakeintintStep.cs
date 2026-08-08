using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.skipTake(...).</summary>
    public sealed class SkipTakeintintStep : IStep
    {
        private readonly int _skip;
        private readonly int _take;

        public SkipTakeintintStep(int skip, int take)
        {
            _skip = skip;
            _take = take;
        }

        public void Apply(StepBuilder builder) => builder.skipTake(_skip, _take);
    }
}
