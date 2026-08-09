using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.sink(...).</summary>
    public sealed class SinkstringStep : StepBase
    {
        public override int Id { get { return 196691; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
        protected override bool HasSql { get { return false; } }

        private readonly string _connector;

        public SinkstringStep(string connector)
        {
            _connector = connector;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_connector);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.sink(_connector);
    }
}
