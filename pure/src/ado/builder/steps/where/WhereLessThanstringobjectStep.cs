using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLessThan(...).</summary>
    public sealed class WhereLessThanstringobjectStep : IStep
    {
        private readonly string _key;
        private readonly object _val;

        public WhereLessThanstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereLessThan(_key, _val);
    }
}
