using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.pinLeft().</summary>
    public sealed class PinLeftStep : StepBase
    {
        public override int Id { get { return 458775; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
        protected override bool HasSql { get { return false; } }

        public static readonly PinLeftStep Instance = new PinLeftStep();
        private PinLeftStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.pinLeft();
    }
}
