using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereInGuid(...).</summary>
    public sealed class WhereInGuidstringEnumGuidStep : WhereListStep
    {
        public override int Id { get { return 196711; } }

        private readonly IEnumerable<Guid> _OIDs;

        public WhereInGuidstringEnumGuidStep(string key, IEnumerable<Guid> OIDs)
            : base(key, OIDs)
        {
            _OIDs = OIDs;
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.whereInGuid(Key, _OIDs);
    }
}
