using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.toggleToUnionOutor().</summary>
    public sealed class ToggleToUnionOutorStep : StepBase
    {
        public override int Id { get { return 327748; } }
        public override StepKind Kind { get { return StepKind.Union; } }

        public static readonly ToggleToUnionOutorStep Instance = new ToggleToUnionOutorStep();
        private ToggleToUnionOutorStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.toggleToUnionOutor();
    }
}
