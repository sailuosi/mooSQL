using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.groupBy(...).</summary>
    public sealed class GroupBystringStep : StepBase
    {
        public override int Id { get { return 65566; } }
        public override StepKind Kind { get { return StepKind.GroupBy; } }

        private readonly string _groupField;

        public GroupBystringStep(string groupField)
        {
            _groupField = groupField;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_groupField);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.groupBy(_groupField);
    }
}
