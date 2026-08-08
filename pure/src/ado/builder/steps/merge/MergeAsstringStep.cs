using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.mergeAs(...).</summary>
    public sealed class MergeAsstringStep : IStep
    {
        private readonly string _asName;

        public MergeAsstringStep(string asName)
        {
            _asName = asName;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.mergeAs(_asName);
    }
}
