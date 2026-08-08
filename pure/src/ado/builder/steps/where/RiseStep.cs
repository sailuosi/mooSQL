using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rise().</summary>
    public sealed class RiseStep : IStep
    {
        public static readonly RiseStep Instance = new RiseStep();
        private RiseStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.rise();
    }
}
