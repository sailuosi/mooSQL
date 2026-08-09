using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.mergeAs(...).</summary>
    public sealed class MergeAsstringStep : StepBase
    {
        public override int Id { get { return 393228; } }
        public override StepKind Kind { get { return StepKind.Merge; } }

        private readonly string _asName;

        public MergeAsstringStep(string asName)
        {
            _asName = asName;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_asName);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.mergeAs(_asName);
    }
}
