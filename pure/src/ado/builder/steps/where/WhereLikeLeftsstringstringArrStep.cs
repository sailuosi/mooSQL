using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLikeLefts(...).</summary>
    public sealed class WhereLikeLeftsstringstringArrStep : StepBase {
        public override int Id { get { return 196724; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly string[] _likeCodes;

        public WhereLikeLeftsstringstringArrStep(string key, params string[] likeCodes)
        {
            _key = key;
            _likeCodes = likeCodes;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereLikeLefts(_key, _likeCodes);
    }
}
