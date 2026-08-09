using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.pinRight().</summary>
    public sealed class PinRightStep : StepBase
    {
        public override int Id { get { return 458776; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
        protected override bool HasSql { get { return false; } }

        public static readonly PinRightStep Instance = new PinRightStep();
        private PinRightStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.pinRight();
    }
}
