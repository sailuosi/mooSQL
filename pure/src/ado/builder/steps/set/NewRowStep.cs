using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.newRow().</summary>
    public sealed class NewRowStep : StepBase
    {
        public override int Id { get { return 262196; } }
        public override StepKind Kind { get { return StepKind.SetRow; } }
        protected override bool HasSql { get { return false; } }

        public static readonly NewRowStep Instance = new NewRowStep();
        private NewRowStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.newRow();
    }
}
