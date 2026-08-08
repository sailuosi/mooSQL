using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.top(...).</summary>
    public sealed class TopintStep : IStep
    {
        private readonly int _num;

        public TopintStep(int num)
        {
            _num = num;
        }

        public void Apply(StepBuilder builder) => builder.top(_num);
    }
}
