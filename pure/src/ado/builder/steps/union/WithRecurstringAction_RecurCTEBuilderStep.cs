using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class WithRecurstringAction_RecurCTEBuilderStep : IStep
    {
        private readonly string _name;
        private readonly Action<RecurCTEBuilder> _buildRecur;

        public WithRecurstringAction_RecurCTEBuilderStep(string name, Action<RecurCTEBuilder> buildRecur)
        {
            _name = name;
            _buildRecur = buildRecur;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.withRecur(_name, _buildRecur);
    }
}
