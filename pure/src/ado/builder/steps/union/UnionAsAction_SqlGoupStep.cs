using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.unionAs(...).</summary>
    public sealed class UnionAsAction_SqlGoupStep : IStep
    {
        private readonly Action<SqlGoup> _dogroup;

        public UnionAsAction_SqlGoupStep(Action<SqlGoup> dogroup)
        {
            _dogroup = dogroup;
        }

        public void Apply(StepBuilder builder) => builder.unionAs(_dogroup);
    }
}
