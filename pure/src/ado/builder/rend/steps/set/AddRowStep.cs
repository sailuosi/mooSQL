using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.addRow().</summary>
    public sealed class AddRowStep : StepBase
    {
        public override int Id { get { return 262194; } }
        public override StepKind Kind { get { return StepKind.SetRow; } }
                public static readonly AddRowStep Instance = new AddRowStep();
        private AddRowStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.addRow();
    }
}
