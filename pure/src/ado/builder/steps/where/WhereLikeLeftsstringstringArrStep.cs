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
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                return;
            }
            var emit = true;
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereLikeLefts(_key, _likeCodes);
    }
}
