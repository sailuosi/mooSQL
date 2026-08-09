using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereInGuid(...).</summary>
    public sealed class WhereInGuidstringEnumGuidNStep : StepBase {
        public override int Id { get { return 196710; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly IEnumerable<Guid?> _OIDs;

        public WhereInGuidstringEnumGuidNStep(string key, IEnumerable<Guid?> OIDs)
        {
            _key = key;
            _OIDs = OIDs;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereInGuid(_key, _OIDs);
    }
}
