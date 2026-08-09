using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.selectSummary(...).</summary>
    public sealed class SelectSummarystringStep : StepBase
    {
        public override int Id { get { return 65577; } }
        public override StepKind Kind { get { return StepKind.SelectMisc; } }

        private readonly string _queryOther;

        public SelectSummarystringStep(string queryOther)
        {
            _queryOther = queryOther;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_queryOther);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.selectSummary(_queryOther);
    }
}
