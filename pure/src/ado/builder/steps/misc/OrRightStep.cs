using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.orRight().</summary>
    public sealed class OrRightStep : IStep
    {
        public static readonly OrRightStep Instance = new OrRightStep();
        private OrRightStep() { }
        public void Apply(StepBuilder builder) => builder.orRight();
    }
}
