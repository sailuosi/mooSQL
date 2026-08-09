using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.and().</summary>
    public sealed class AndStep : StepBase
    {
        public override int Id { get { return 196683; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
        protected override bool HasSql { get { return false; } }

        public static readonly AndStep Instance = new AndStep();
        private AndStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.and();
    }
}
