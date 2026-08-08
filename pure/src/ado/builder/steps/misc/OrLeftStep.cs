using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.orLeft().</summary>
    public sealed class OrLeftStep : IStep
    {
        public static readonly OrLeftStep Instance = new OrLeftStep();
        private OrLeftStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.orLeft();
    }
}
