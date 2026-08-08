using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.copyPreFrom().</summary>
    public sealed class CopyPreFromStep : IStep
    {
        public static readonly CopyPreFromStep Instance = new CopyPreFromStep();
        private CopyPreFromStep() { }
        public void Apply(StepBuilder builder) => builder.copyPreFrom();
    }
}
