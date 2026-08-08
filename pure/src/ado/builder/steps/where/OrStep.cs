using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.or().</summary>
    public sealed class OrStep : IStep
    {
        public static readonly OrStep Instance = new OrStep();
        private OrStep() { }
        public void Apply(StepBuilder builder) => builder.or();
    }
}
