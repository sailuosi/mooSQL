using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.not().</summary>
    public sealed class NotStep : IStep
    {
        public static readonly NotStep Instance = new NotStep();
        private NotStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.not();
    }
}
