using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIn(...).</summary>
    public sealed class WhereInstringEnumStep : IStep
    {
        private readonly string _key;
        private readonly IEnumerable _values;

        public WhereInstringEnumStep(string key, IEnumerable values)
        {
            _key = key;
            _values = values;
        }

        public void Apply(StepBuilder builder) => builder.whereIn(_key, _values);
    }
}
