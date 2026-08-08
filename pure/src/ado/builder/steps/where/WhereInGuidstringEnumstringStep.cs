using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereInGuid(...).</summary>
    public sealed class WhereInGuidstringEnumstringStep : IStep
    {
        private readonly string _key;
        private readonly IEnumerable<string> _OIDs;

        public WhereInGuidstringEnumstringStep(string key, IEnumerable<string> OIDs)
        {
            _key = key;
            _OIDs = OIDs;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereInGuid(_key, _OIDs);
    }
}
