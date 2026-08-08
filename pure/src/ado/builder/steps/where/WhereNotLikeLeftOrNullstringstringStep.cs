using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotLikeLeftOrNull(...).</summary>
    public sealed class WhereNotLikeLeftOrNullstringstringStep : IStep
    {
        private readonly string _key;
        private readonly string _val;

        public WhereNotLikeLeftOrNullstringstringStep(string key, string val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereNotLikeLeftOrNull(_key, _val);
    }
}
