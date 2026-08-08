using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIf(...).</summary>
    public sealed class WhereIfboolNstringStep : IStep
    {
        private readonly bool? _isTrue;
        private readonly string _key;

        public WhereIfboolNstringStep(bool? isTrue, string key)
        {
            _isTrue = isTrue;
            _key = key;
        }

        public void Apply(StepBuilder builder) => builder.whereIf(_isTrue, _key);
    }
}
