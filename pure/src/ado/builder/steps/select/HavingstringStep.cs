using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.having(...).</summary>
    public sealed class HavingstringStep : StepBase
    {
        public override int Id { get { return 65567; } }
        public override StepKind Kind { get { return StepKind.Having; } }

        private readonly string _havingStr;

        public HavingstringStep(string havingStr)
        {
            _havingStr = havingStr;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_havingStr);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.having(_havingStr);
    }
}
