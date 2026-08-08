using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereVsOrNull(...).</summary>
    public sealed class WhereVsOrNullstringobjectstringStep : IStep
    {
        private readonly string _key;
        private readonly object _val;
        private readonly string _op;

        public WhereVsOrNullstringobjectstringStep(string key, object val, string op)
        {
            _key = key;
            _val = val;
            _op = op;
        }

        public void Apply(StepBuilder builder) => builder.whereVsOrNull(_key, _val, _op);
    }
}
