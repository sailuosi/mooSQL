using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.andLeft().</summary>
    public sealed class AndLeftStep : StepBase
    {
        public override int Id { get { return 458768; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
        protected override bool HasSql { get { return false; } }

        public static readonly AndLeftStep Instance = new AndLeftStep();
        private AndLeftStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.andLeft();
    }
}
