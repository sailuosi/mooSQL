using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.mergeOn(...).</summary>
    public sealed class MergeOnstringStep : IStep
    {
        private readonly string _onPart;

        public MergeOnstringStep(string onPart)
        {
            _onPart = onPart;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.mergeOn(_onPart);
    }
}
