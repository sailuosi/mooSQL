using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.skipTake(...).</summary>
    public sealed class SkipTakeintintStep : StepBase
    {
        public override int Id { get { return 65582; } }
        public override StepKind Kind { get { return StepKind.TopSkipTake; } }

        private readonly int _skip;
        private readonly int _take;

        public SkipTakeintintStep(int skip, int take)
        {
            _skip = skip;
            _take = take;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_skip);
            hc.Add(_take);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.skipTake(_skip, _take);
    }
}
