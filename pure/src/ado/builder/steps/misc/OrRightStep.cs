using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.orRight().</summary>
    public sealed class OrRightStep : StepBase
    {
        public override int Id { get { return 458774; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
        protected override bool HasSql { get { return false; } }

        public static readonly OrRightStep Instance = new OrRightStep();
        private OrRightStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.orRight();
    }
}
