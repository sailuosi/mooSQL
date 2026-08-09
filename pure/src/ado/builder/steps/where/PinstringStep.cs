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
                private readonly string _SQL;

        public PinstringStep(string SQL)
        {
            _SQL = SQL;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
            hc.Add(_SQL);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.pin(_SQL);
    }
}
