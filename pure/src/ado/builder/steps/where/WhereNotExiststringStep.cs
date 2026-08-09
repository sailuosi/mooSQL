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
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_selectSQL);
                return;
            }
            var emit = true;
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_selectSQL);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereNotExist(_selectSQL);
    }
}
