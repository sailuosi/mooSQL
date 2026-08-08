using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereInGuid(...).</summary>
    public sealed class WhereInGuidstringEnumGuidNStep : IStep
    {
        private readonly string _key;
        private readonly IEnumerable<Guid?> _OIDs;

        public WhereInGuidstringEnumGuidNStep(string key, IEnumerable<Guid?> OIDs)
        {
            _key = key;
            _OIDs = OIDs;
        }

        public void Apply(StepBuilder builder) => builder.whereInGuid(_key, _OIDs);
    }
}
