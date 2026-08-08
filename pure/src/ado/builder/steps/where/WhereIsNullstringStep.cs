using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIsNull(...).</summary>
    public sealed class WhereIsNullstringStep : IStep
    {
        private readonly string _key;

        public WhereIsNullstringStep(string key)
        {
            _key = key;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereIsNull(_key);
    }
}
