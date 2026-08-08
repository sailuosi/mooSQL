using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.sink(...).</summary>
    public sealed class SinkstringStep : IStep
    {
        private readonly string _connector;

        public SinkstringStep(string connector)
        {
            _connector = connector;
        }

        public void Apply(StepBuilder builder) => builder.sink(_connector);
    }
}
