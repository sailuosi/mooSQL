using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.copyPreFrom().</summary>
    public sealed class CopyPreFromStep : StepBase
    {
        public override int Id { get { return 65561; } }
        public override StepKind Kind { get { return StepKind.SelectMisc; } }

        public static readonly CopyPreFromStep Instance = new CopyPreFromStep();
        private CopyPreFromStep() { }
        public override void Apply(SQLBuilder builder) => builder.Inner.copyPreFrom();
    }
}
