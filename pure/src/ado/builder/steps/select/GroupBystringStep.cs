using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.groupBy(...).</summary>
    public sealed class GroupBystringStep : IStep
    {
        private readonly string _groupField;

        public GroupBystringStep(string groupField)
        {
            _groupField = groupField;
        }

        public void Apply(StepBuilder builder) => builder.groupBy(_groupField);
    }
}
