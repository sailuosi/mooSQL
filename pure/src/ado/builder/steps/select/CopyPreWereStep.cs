using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.copyPreWere().</summary>
    public sealed class CopyPreWereStep : IStep
    {
        public static readonly CopyPreWereStep Instance = new CopyPreWereStep();
        private CopyPreWereStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.copyPreWere();
    }
}
