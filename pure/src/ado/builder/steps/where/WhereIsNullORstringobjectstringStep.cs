using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIsNullOR(...).</summary>
    public sealed class WhereIsNullORstringobjectstringStep : IStep
    {
        private readonly string _key;
        private readonly object _val;
        private readonly string _op;

        public WhereIsNullORstringobjectstringStep(string key, object val, string op)
        {
            _key = key;
            _val = val;
            _op = op;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereIsNullOR(_key, _val, _op);
    }
}
