using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereGuid(...).</summary>
    public sealed class WhereGuidstringobjectStep : IStep
    {
        private readonly string _key;
        private readonly object _val;

        public WhereGuidstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(StepBuilder builder) => builder.whereGuid(_key, _val);
    }
}
