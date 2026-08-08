using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.sinkNot(...).</summary>
    public sealed class SinkNotstringStep : IStep
    {
        private readonly string _connector;

        public SinkNotstringStep(string connector)
        {
            _connector = connector;
        }

        public void Apply(StepBuilder builder) => builder.sinkNot(_connector);
    }
}
