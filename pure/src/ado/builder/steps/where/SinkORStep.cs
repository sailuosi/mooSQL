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
        protected override bool HasSql { get { return false; } }

        public static readonly SinkORStep Instance = new SinkORStep();
        private SinkORStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.sinkOR();
    }
}
