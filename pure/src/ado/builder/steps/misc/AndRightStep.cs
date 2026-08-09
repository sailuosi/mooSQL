using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.andRight().</summary>
    public sealed class AndRightStep : StepBase
    {
        public override int Id { get { return 458769; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
                public static readonly AndRightStep Instance = new AndRightStep();
        private AndRightStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.andRight();
    }
}
