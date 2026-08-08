using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotExist(...).</summary>
    public sealed class WhereNotExistActStep : IStep
    {
        private readonly Action<SQLBuilder> _doselect;

        public WhereNotExistActStep(Action<SQLBuilder> doselect)
        {
            _doselect = doselect;
        }

        public void Apply(StepBuilder builder) => builder.whereNotExist(_doselect);
    }
}
