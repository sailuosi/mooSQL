using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereInGuid(...).</summary>
    public sealed class WhereInGuidstringEnumGuidStep : StepBase {
        public override int Id { get { return 196711; } }
        public override StepKind Kind { get { return StepKind.Where; } }
                private readonly string _key;
        private readonly IEnumerable<Guid> _OIDs;

        public WhereInGuidstringEnumGuidStep(string key, IEnumerable<Guid> OIDs)
        {
            _key = key;
            _OIDs = OIDs;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                return;
            }
            bool emit;
            if (paraRule == "all") emit = true;
            else if (paraRule == "notNull") emit = _OIDs != null;
            else emit = CollectionHasAny(_OIDs);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereInGuid(_key, _OIDs);
    }
}
