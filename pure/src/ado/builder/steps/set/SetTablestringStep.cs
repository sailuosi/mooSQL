using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setTable(...).</summary>
    public sealed class SetTablestringStep : StepBase
    {
        public override int Id { get { return 262201; } }
        public override StepKind Kind { get { return StepKind.SetTable; } }

        private readonly string _tbName;

        public SetTablestringStep(string tbName)
        {
            _tbName = tbName;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_tbName);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.setTable(_tbName);
    }
}
