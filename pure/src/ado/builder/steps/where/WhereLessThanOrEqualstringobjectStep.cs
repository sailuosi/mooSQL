using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLessThanOrEqual(...).</summary>
    public sealed class WhereLessThanOrEqualstringobjectStep : IStep
    {
        private readonly string _key;
        private readonly object _val;

        public WhereLessThanOrEqualstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereLessThanOrEqual(_key, _val);
    }
}
