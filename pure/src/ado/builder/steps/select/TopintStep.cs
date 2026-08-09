using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.top(...).</summary>
    public sealed class TopintStep : StepBase
    {
        public override int Id { get { return 65585; } }
        public override StepKind Kind { get { return StepKind.TopSkipTake; } }

        private readonly int _num;

        public TopintStep(int num)
        {
            _num = num;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_num);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.top(_num);
    }
}
