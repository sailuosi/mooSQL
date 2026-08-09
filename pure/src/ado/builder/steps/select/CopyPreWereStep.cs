using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.copyPreWere().</summary>
    public sealed class CopyPreWereStep : StepBase
    {
        public override int Id { get { return 65563; } }
        public override StepKind Kind { get { return StepKind.SelectMisc; } }

        public static readonly CopyPreWereStep Instance = new CopyPreWereStep();
        private CopyPreWereStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.copyPreWere();
    }
}
