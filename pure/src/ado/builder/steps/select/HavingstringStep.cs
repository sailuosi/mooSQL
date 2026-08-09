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
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_havingStr);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.having(_havingStr);
    }
}
