using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereExist(...).</summary>
    public sealed class WhereExiststringStep : IStep
    {
        private readonly string _value;

        public WhereExiststringStep(string value)
        {
            _value = value;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereExist(_value);
    }
}
