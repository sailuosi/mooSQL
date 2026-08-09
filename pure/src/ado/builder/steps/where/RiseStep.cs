using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rise().</summary>
    public sealed class RiseStep : StepBase
    {
        public override int Id { get { return 196687; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
        protected override bool HasSql { get { return false; } }

        public static readonly RiseStep Instance = new RiseStep();
        private RiseStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.rise();
    }
}
