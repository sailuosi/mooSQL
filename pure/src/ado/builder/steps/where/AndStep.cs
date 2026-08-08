using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.and().</summary>
    public sealed class AndStep : IStep
    {
        public static readonly AndStep Instance = new AndStep();
        private AndStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.and();
    }
}
