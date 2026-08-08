using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotLikeLefts(...).</summary>
    public sealed class WhereNotLikeLeftsstringEnumstringStep : IStep
    {
        private readonly string _key;
        private readonly IEnumerable<string> _vals;

        public WhereNotLikeLeftsstringEnumstringStep(string key, IEnumerable<string> vals)
        {
            _key = key;
            _vals = vals;
        }

        public void Apply(StepBuilder builder) => builder.whereNotLikeLefts(_key, _vals);
    }
}
