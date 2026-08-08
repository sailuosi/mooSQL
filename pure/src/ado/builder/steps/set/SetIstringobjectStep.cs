using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setI(...).</summary>
    public sealed class SetIstringobjectStep : IStep
    {
        private readonly string _key;
        private readonly object _val;

        public SetIstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.setI(_key, _val);
    }
}
