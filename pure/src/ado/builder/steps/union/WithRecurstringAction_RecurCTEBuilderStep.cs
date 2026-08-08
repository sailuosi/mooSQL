using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.withRecur(...).</summary>
    public sealed class WithRecurstringAction_RecurCTEBuilderStep : IStep
    {
        private readonly string _name;
        private readonly Action<RecurCTEBuilder> _buildRecur;

        public WithRecurstringAction_RecurCTEBuilderStep(string name, Action<RecurCTEBuilder> buildRecur)
        {
            _name = name;
            _buildRecur = buildRecur;
        }

        public void Apply(StepBuilder builder) => builder.withRecur(_name, _buildRecur);
    }
}
