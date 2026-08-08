using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.newRow().</summary>
    public sealed class NewRowStep : IStep
    {
        public static readonly NewRowStep Instance = new NewRowStep();
        private NewRowStep() { }
        public void Apply(StepBuilder builder) => builder.newRow();
    }
}
