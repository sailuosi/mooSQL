using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotLikeLeft(...).</summary>
    public sealed class WhereNotLikeLeftstringstringStep : IStep
    {
        private readonly string _key;
        private readonly string _val;

        public WhereNotLikeLeftstringstringStep(string key, string val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereNotLikeLeft(_key, _val);
    }
}
