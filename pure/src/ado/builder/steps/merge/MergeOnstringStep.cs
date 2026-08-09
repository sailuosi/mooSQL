using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.mergeOn(...).</summary>
    public sealed class MergeOnstringStep : StepBase
    {
        public override int Id { get { return 393230; } }
        public override StepKind Kind { get { return StepKind.Merge; } }

        private readonly string _onPart;

        public MergeOnstringStep(string onPart)
        {
            _onPart = onPart;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_onPart);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.mergeOn(_onPart);
    }
}
