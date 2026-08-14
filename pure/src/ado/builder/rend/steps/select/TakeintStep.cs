using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.take(...).</summary>
    public sealed class TakeintStep : StepBase
    {
        public override int Id { get { return 65584; } }
        public override StepKind Kind { get { return StepKind.TopSkipTake; } }

        private readonly int _skip;

        public TakeintStep(int skip)
        {
            _skip = skip;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_skip);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.take(_skip);
    }
}
