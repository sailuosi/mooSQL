using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.addRow().</summary>
    public sealed class AddRowStep : IStep
    {
        public static readonly AddRowStep Instance = new AddRowStep();
        private AddRowStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.addRow();
    }
}
