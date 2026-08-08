using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereGreaterThan(...).</summary>
    public sealed class WhereGreaterThanstringobjectStep : IStep
    {
        private readonly string _key;
        private readonly object _val;

        public WhereGreaterThanstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(StepBuilder builder) => builder.whereGreaterThan(_key, _val);
    }
}
