using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.prefix(...).</summary>
    public sealed class PrefixstringStep : IStep
    {
        private readonly string _SQLString;

        public PrefixstringStep(string SQLString)
        {
            _SQLString = SQLString;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.prefix(_SQLString);
    }
}
