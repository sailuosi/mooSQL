using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.sinkNotOR().</summary>
    public sealed class SinkNotORStep : IStep
    {
        public static readonly SinkNotORStep Instance = new SinkNotORStep();
        private SinkNotORStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.sinkNotOR();
    }
}
