using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotIn(...).</summary>
    public sealed class WhereNotInstringEnumStep : StepBase {
        public override int Id { get { return 196733; } }
        public override StepKind Kind { get { return StepKind.Where; } }
                private readonly string _key;
        private readonly IEnumerable _values;

        public WhereNotInstringEnumStep(string key, IEnumerable values)
        {
            _key = key;
            _values = values;
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
            else if (paraRule == "notNull") emit = _values != null;
            else emit = CollectionHasAny(_values);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereNotIn(_key, _values);
    }
}
