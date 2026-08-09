using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class UnionAsAction_SqlGoupStep : StepBase
    {
        public override int Id { get { return 327751; } }
        public override StepKind Kind { get { return StepKind.Union; } }

        private readonly Action<SqlGoup> _dogroup;

        public UnionAsAction_SqlGoupStep(Action<SqlGoup> dogroup)
        {
            _dogroup = dogroup;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.unionAs(_dogroup);
    }
}
