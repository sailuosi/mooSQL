using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rowNumber().</summary>
    public sealed class RowNumberStep : IStep
    {
        public static readonly RowNumberStep Instance = new RowNumberStep();
        private RowNumberStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.rowNumber();
    }
}
