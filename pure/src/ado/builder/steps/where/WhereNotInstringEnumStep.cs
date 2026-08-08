using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotIn(...).</summary>
    public sealed class WhereNotInstringEnumStep : IStep
    {
        private readonly string _key;
        private readonly IEnumerable _values;

        public WhereNotInstringEnumStep(string key, IEnumerable values)
        {
            _key = key;
            _values = values;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereNotIn(_key, _values);
    }
}
