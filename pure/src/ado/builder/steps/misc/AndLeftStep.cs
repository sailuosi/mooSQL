using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.andLeft().</summary>
    public sealed class AndLeftStep : StepBase
    {
        public override int Id { get { return 458768; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
                public static readonly AndLeftStep Instance = new AndLeftStep();
        private AndLeftStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.andLeft();
    }
}
