using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLikesAnd(...).</summary>
    public sealed class WhereLikesAndstringstringArrStep : StepBase {
        public override int Id { get { return 196726; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly string[] _vals;

        public WhereLikesAndstringstringArrStep(string key, params string[] vals)
        {
            _key = key;
            _vals = vals;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereLikesAnd(_key, _vals);
    }
}
