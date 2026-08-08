using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(...).</summary>
    public sealed class WherestringobjectstringTypeStep : IStep
    {
        private readonly string _key;
        private readonly object _val;
        private readonly string _op;
        private readonly Type _t;

        public WherestringobjectstringTypeStep(string key, object val, string op, Type t)
        {
            _key = key;
            _val = val;
            _op = op;
            _t = t;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.where(_key, _val, _op, _t);
    }
}
