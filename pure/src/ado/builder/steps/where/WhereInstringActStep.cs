using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIn(...).</summary>
    public sealed class WhereInstringActStep : IStep
    {
        private readonly string _key;
        private readonly Action<SQLBuilder> _doselect;

        public WhereInstringActStep(string key, Action<SQLBuilder> doselect)
        {
            _key = key;
            _doselect = doselect;
        }

        public void Apply(StepBuilder builder) => builder.whereIn(_key, _doselect);
    }
}
