using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rowNumberUse(...).</summary>
    public sealed class RowNumberUsestringStep : IStep
    {
        private readonly string _numFieldName;

        public RowNumberUsestringStep(string numFieldName)
        {
            _numFieldName = numFieldName;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.rowNumberUse(_numFieldName);
    }
}
