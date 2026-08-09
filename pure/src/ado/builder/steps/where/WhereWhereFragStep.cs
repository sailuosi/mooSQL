using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(...).</summary>
    public sealed class WhereWhereFragStep : StepBase {
        public override int Id { get { return 196744; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly WhereFrag _frag;

        public WhereWhereFragStep(WhereFrag frag)
        {
            _frag = frag;
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.where(_frag);
    }
}
