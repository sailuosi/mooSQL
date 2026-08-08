using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.andRight().</summary>
    public sealed class AndRightStep : IStep
    {
        public static readonly AndRightStep Instance = new AndRightStep();
        private AndRightStep() { }
        public void Apply(StepBuilder builder) => builder.andRight();
    }
}
