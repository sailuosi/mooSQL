using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class WithRecurstringAction_RecurCTEBuilderStep : StepBase
    {
        public override int Id { get { return 327753; } }
        public override StepKind Kind { get { return StepKind.Cte; } }

        private readonly string _name;
        private readonly Action<RecurCTEBuilder> _buildRecur;

        public WithRecurstringAction_RecurCTEBuilderStep(string name, Action<RecurCTEBuilder> buildRecur)
        {
            _name = name;
            _buildRecur = buildRecur;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_name);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.withRecur(_name, _buildRecur);
    }
}
