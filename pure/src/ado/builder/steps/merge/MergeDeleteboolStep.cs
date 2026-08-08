using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.mergeDelete(...).</summary>
    public sealed class MergeDeleteboolStep : IStep
    {
        private readonly bool _thenDelete;

        public MergeDeleteboolStep(bool thenDelete)
        {
            _thenDelete = thenDelete;
        }

        public void Apply(StepBuilder builder) => builder.mergeDelete(_thenDelete);
    }
}
