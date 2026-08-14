using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.sinkOR().</summary>
    public sealed class SinkORStep : StepBase
    {
        public override int Id { get { return 196690; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
                public static readonly SinkORStep Instance = new SinkORStep();
        private SinkORStep() { }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }
        public override void Apply(SQLBuilder builder) => builder.Inner.sinkOR();
    }
}
