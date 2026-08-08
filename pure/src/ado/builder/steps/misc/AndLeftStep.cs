using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.andLeft().</summary>
    public sealed class AndLeftStep : IStep
    {
        public static readonly AndLeftStep Instance = new AndLeftStep();
        private AndLeftStep() { }
        public void Apply(StepBuilder builder) => builder.andLeft();
    }
}
