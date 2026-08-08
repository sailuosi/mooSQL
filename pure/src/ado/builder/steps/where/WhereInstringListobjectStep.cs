using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIn(...).</summary>
    public sealed class WhereInstringListobjectStep : IStep
    {
        private readonly string _key;
        private readonly List<object> _val;

        public WhereInstringListobjectStep(string key, List<object> val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(StepBuilder builder) => builder.whereIn(_key, _val);
    }
}
