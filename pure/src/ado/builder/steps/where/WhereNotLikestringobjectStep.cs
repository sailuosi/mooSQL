using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotLike(...).</summary>
    public sealed class WhereNotLikestringobjectStep : IStep
    {
        private readonly string _key;
        private readonly object _val;

        public WhereNotLikestringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(StepBuilder builder) => builder.whereNotLike(_key, _val);
    }
}
