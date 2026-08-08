using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereGreaterThanOrEqual(...).</summary>
    public sealed class WhereGreaterThanOrEqualstringobjectStep : IStep
    {
        private readonly string _key;
        private readonly object _val;

        public WhereGreaterThanOrEqualstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereGreaterThanOrEqual(_key, _val);
    }
}
