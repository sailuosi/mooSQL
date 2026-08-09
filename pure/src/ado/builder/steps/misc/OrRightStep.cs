using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.orRight().</summary>
    public sealed class OrRightStep : StepBase
    {
        public override int Id { get { return 458774; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
                public static readonly OrRightStep Instance = new OrRightStep();
        private OrRightStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.orRight();
    }
}
