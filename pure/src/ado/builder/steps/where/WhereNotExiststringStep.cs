using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotExist(...).</summary>
    public sealed class WhereNotExiststringStep : IStep
    {
        private readonly string _selectSQL;

        public WhereNotExiststringStep(string selectSQL)
        {
            _selectSQL = selectSQL;
        }

        public void Apply(StepBuilder builder) => builder.whereNotExist(_selectSQL);
    }
}
