using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.sinkNot(...).</summary>
    public sealed class SinkNotstringStep : StepBase
    {
        public override int Id { get { return 196689; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
        protected override bool HasSql { get { return false; } }

        private readonly string _connector;

        public SinkNotstringStep(string connector)
        {
            _connector = connector;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_connector);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.sinkNot(_connector);
    }
}
