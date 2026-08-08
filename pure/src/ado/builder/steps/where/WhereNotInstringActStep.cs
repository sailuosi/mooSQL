using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotIn(...).</summary>
    public sealed class WhereNotInstringActStep : IStep
    {
        private readonly string _key;
        private readonly Action<SQLBuilder> _doselect;

        public WhereNotInstringActStep(string key, Action<SQLBuilder> doselect)
        {
            _key = key;
            _doselect = doselect;
        }

        public void Apply(StepBuilder builder) => builder.whereNotIn(_key, _doselect);
    }
}
