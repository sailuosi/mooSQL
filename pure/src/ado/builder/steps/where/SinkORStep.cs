using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.sinkOR().</summary>
    public sealed class SinkORStep : IStep
    {
        public static readonly SinkORStep Instance = new SinkORStep();
        private SinkORStep() { }
        public void Apply(StepBuilder builder) => builder.sinkOR();
    }
}
