using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotEqual(...).</summary>
    public sealed class WhereNotEqualstringobjectStep : IStep
    {
        private readonly string _key;
        private readonly object _val;

        public WhereNotEqualstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(StepBuilder builder) => builder.whereNotEqual(_key, _val);
    }
}
