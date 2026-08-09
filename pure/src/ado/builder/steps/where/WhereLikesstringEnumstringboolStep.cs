using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLikes(...).</summary>
    public sealed class WhereLikesstringEnumstringboolStep : StepBase {
        public override int Id { get { return 196729; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly IEnumerable<string> _vals;
        private readonly bool _isOr;

        public WhereLikesstringEnumstringboolStep(string key, IEnumerable<string> vals, bool isOr)
        {
            _key = key;
            _vals = vals;
            _isOr = isOr;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
            hc.Add(_isOr);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereLikes(_key, _vals, _isOr);
    }
}
