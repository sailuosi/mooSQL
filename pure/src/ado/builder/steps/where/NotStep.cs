using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.not().</summary>
    public sealed class NotStep : StepBase
    {
        public override int Id { get { return 196684; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
                public static readonly NotStep Instance = new NotStep();
        private NotStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.not();
    }
}
