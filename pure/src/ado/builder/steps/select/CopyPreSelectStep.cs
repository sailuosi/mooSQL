using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.copyPreSelect().</summary>
    public sealed class CopyPreSelectStep : IStep
    {
        public static readonly CopyPreSelectStep Instance = new CopyPreSelectStep();
        private CopyPreSelectStep() { }
        public void Apply(StepBuilder builder) => builder.copyPreSelect();
    }
}
