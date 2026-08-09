using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.innerJoin(...).</summary>
    public sealed class InnerJoinstringStep : StepBase
    {
        public override int Id { get { return 131075; } }
        public override StepKind Kind { get { return StepKind.Join; } }

        private readonly string _joinSQLString;

        public InnerJoinstringStep(string joinSQLString)
        {
            _joinSQLString = joinSQLString;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_joinSQLString);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.innerJoin(_joinSQLString);
    }
}
