using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.mergeUsing(...).</summary>
    public sealed class MergeUsingstringActStep : IStep
    {
        private readonly string _asName;
        private readonly Action<SQLBuilder> _buildSelect;

        public MergeUsingstringActStep(string asName, Action<SQLBuilder> buildSelect)
        {
            _asName = asName;
            _buildSelect = buildSelect;
        }

        public void Apply(StepBuilder builder) => builder.mergeUsing(_asName, _buildSelect);
    }
}
