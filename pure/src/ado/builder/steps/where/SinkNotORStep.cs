using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.sinkNotOR().</summary>
    public sealed class SinkNotORStep : StepBase
    {
        public override int Id { get { return 196688; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
        protected override bool HasSql { get { return false; } }

        public static readonly SinkNotORStep Instance = new SinkNotORStep();
        private SinkNotORStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.sinkNotOR();
    }
}
