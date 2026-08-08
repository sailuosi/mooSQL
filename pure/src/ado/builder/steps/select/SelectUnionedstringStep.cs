using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.selectUnioned(...).</summary>
    public sealed class SelectUnionedstringStep : IStep
    {
        private readonly string _columns;

        public SelectUnionedstringStep(string columns)
        {
            _columns = columns;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.selectUnioned(_columns);
    }
}
