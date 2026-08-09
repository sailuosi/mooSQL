using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIn(...).</summary>
    public sealed class WhereInstringListobjectStep : StepBase {
        public override int Id { get { return 196714; } }
        public override StepKind Kind { get { return StepKind.Where; } }
                private readonly string _key;
        private readonly List<object> _val;

        public WhereInstringListobjectStep(string key, List<object> val)
        {
            _key = key;
            _val = val;
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
            else if (paraRule == "notNull") emit = _val != null;
            else emit = CollectionHasAny(_val);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereIn(_key, _val);
    }
}
