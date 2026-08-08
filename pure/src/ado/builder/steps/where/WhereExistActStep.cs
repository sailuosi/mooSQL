using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereExist(...).</summary>
    public sealed class WhereExistActStep : IStep
    {
        private readonly Action<SQLBuilder> _doselect;

        public WhereExistActStep(Action<SQLBuilder> doselect)
        {
            _doselect = doselect;
        }

        public void Apply(StepBuilder builder) => builder.whereExist(_doselect);
    }
}
