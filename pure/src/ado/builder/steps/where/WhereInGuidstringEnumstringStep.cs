using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereInGuid(...).</summary>
    public sealed class WhereInGuidstringEnumstringStep : WhereListStep
    {
        public override int Id { get { return 196712; } }

        private readonly IEnumerable<string> _OIDs;

        public WhereInGuidstringEnumstringStep(string key, IEnumerable<string> OIDs)
            : base(key, OIDs)
        {
            _OIDs = OIDs;
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.whereInGuid(Key, _OIDs);
    }
}
