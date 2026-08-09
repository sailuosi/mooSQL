using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereInGuid(...).</summary>
    public sealed class WhereInGuidstringEnumstringStep : StepBase {
        public override int Id { get { return 196712; } }
        public override StepKind Kind { get { return StepKind.Where; } }
                private readonly string _key;
        private readonly IEnumerable<string> _OIDs;

        public WhereInGuidstringEnumstringStep(string key, IEnumerable<string> OIDs)
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
