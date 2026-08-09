using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.orLeft().</summary>
    public sealed class OrLeftStep : StepBase
    {
        public override int Id { get { return 458773; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
                public static readonly OrLeftStep Instance = new OrLeftStep();
        private OrLeftStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.orLeft();
    }
}
