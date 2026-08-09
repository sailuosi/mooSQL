using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.copyPreSelect().</summary>
    public sealed class CopyPreSelectStep : StepBase
    {
        public override int Id { get { return 65562; } }
        public override StepKind Kind { get { return StepKind.SelectMisc; } }

        public static readonly CopyPreSelectStep Instance = new CopyPreSelectStep();
        private CopyPreSelectStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.copyPreSelect();
    }
}
