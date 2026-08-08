using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIf(...).</summary>
    public sealed class WhereIfboolNstringobjectstringStep : IStep
    {
        private readonly bool? _isTrue;
        private readonly string _key;
        private readonly object _val;
        private readonly string _op;

        public WhereIfboolNstringobjectstringStep(bool? isTrue, string key, object val, string op)
        {
            _isTrue = isTrue;
            _key = key;
            _val = val;
            _op = op;
        }

        public void Apply(StepBuilder builder) => builder.whereIf(_isTrue, _key, _val, _op);
    }
}
