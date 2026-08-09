using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotExist(...).</summary>
    public sealed class WhereNotExiststringStep : StepBase {
        public override int Id { get { return 196732; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _selectSQL;

        public WhereNotExiststringStep(string selectSQL)
        {
            _selectSQL = selectSQL;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_selectSQL);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereNotExist(_selectSQL);
    }
}
