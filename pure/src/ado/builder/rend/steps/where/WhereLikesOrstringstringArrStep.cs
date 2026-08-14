using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLikesOr(...).</summary>
    public sealed class WhereLikesOrstringstringArrStep : StepBase {
        public override int Id { get { return 196728; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly string[] _vals;

        public WhereLikesOrstringstringArrStep(string key, params string[] vals)
        {
            _key = key;
            _vals = vals;
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
                public override void Apply(SQLBuilder builder) => builder.Inner.whereLikesOr(_key, _vals);
    }
}
