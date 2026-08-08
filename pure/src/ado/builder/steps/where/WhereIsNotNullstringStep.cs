using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIsNotNull(...).</summary>
    public sealed class WhereIsNotNullstringStep : IStep
    {
        private readonly string _key;

        public WhereIsNotNullstringStep(string key)
        {
            _key = key;
        }

        public void Apply(StepBuilder builder) => builder.whereIsNotNull(_key);
    }
}
