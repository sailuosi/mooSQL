using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.orLeft().</summary>
    public sealed class OrLeftStep : StepBase
    {
        public override int Id { get { return 458773; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
        protected override bool HasSql { get { return false; } }

        public static readonly OrLeftStep Instance = new OrLeftStep();
        private OrLeftStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.orLeft();
    }
}
