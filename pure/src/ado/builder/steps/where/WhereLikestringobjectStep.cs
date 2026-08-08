using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLike(...).</summary>
    public sealed class WhereLikestringobjectStep : IStep
    {
        private readonly string _key;
        private readonly object _val;

        public WhereLikestringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereLike(_key, _val);
    }
}
