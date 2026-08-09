using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rowNumber().</summary>
    public sealed class RowNumberStep : StepBase
    {
        public override int Id { get { return 65571; } }
        public override StepKind Kind { get { return StepKind.RowNumber; } }

        public static readonly RowNumberStep Instance = new RowNumberStep();
        private RowNumberStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.rowNumber();
    }
}
