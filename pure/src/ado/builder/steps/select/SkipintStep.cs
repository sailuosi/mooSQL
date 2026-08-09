using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.skip(...).</summary>
    public sealed class SkipintStep : StepBase
    {
        public override int Id { get { return 65581; } }
        public override StepKind Kind { get { return StepKind.TopSkipTake; } }

        private readonly int _skip;

        public SkipintStep(int skip)
        {
            _skip = skip;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_skip);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.skip(_skip);
    }
}
