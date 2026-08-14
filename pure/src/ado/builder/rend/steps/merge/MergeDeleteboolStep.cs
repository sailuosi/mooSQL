using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.mergeDelete(...).</summary>
    public sealed class MergeDeleteboolStep : StepBase
    {
        public override int Id { get { return 393229; } }
        public override StepKind Kind { get { return StepKind.Merge; } }

        private readonly bool _thenDelete;

        public MergeDeleteboolStep(bool thenDelete)
        {
            _thenDelete = thenDelete;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_thenDelete);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.mergeDelete(_thenDelete);
    }
}
