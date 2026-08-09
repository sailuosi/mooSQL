using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.addRow().</summary>
    public sealed class AddRowStep : StepBase
    {
        public override int Id { get { return 262194; } }
        public override StepKind Kind { get { return StepKind.SetRow; } }
        protected override bool HasSql { get { return false; } }

        public static readonly AddRowStep Instance = new AddRowStep();
        private AddRowStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.addRow();
    }
}
