using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setU(...).</summary>
    public sealed class SetUstringobjectStep : IStep
    {
        private readonly string _key;
        private readonly object _val;

        public SetUstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.setU(_key, _val);
    }
}
