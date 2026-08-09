using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.pin(...).</summary>
    public sealed class PinstringStep : StepBase
    {
        public override int Id { get { return 196686; } }
        public override StepKind Kind { get { return StepKind.WhereControl; } }
        protected override bool HasSql { get { return false; } }

        private readonly string _SQL;

        public PinstringStep(string SQL)
        {
            _SQL = SQL;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_SQL);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.pin(_SQL);
    }
}
