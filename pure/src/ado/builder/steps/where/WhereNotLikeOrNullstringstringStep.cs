using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotLikeOrNull(...).</summary>
    public sealed class WhereNotLikeOrNullstringstringStep : IStep
    {
        private readonly string _key;
        private readonly string _val;

        public WhereNotLikeOrNullstringstringStep(string key, string val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(StepBuilder builder) => builder.whereNotLikeOrNull(_key, _val);
    }
}
