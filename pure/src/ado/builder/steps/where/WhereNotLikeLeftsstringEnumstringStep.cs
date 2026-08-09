using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotLikeLefts(...).</summary>
    public sealed class WhereNotLikeLeftsstringEnumstringStep : StepBase {
        public override int Id { get { return 196735; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly IEnumerable<string> _vals;

        public WhereNotLikeLeftsstringEnumstringStep(string key, IEnumerable<string> vals)
        {
            _key = key;
            _vals = vals;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereNotLikeLefts(_key, _vals);
    }
}
