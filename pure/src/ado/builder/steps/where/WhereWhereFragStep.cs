using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(...).</summary>
    public sealed class WhereWhereFragStep : IStep
    {
        private readonly WhereFrag _frag;

        public WhereWhereFragStep(WhereFrag frag)
        {
            _frag = frag;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.where(_frag);
    }
}
