using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.selectWith(...).</summary>
    public sealed class SelectWithstringStep : StepBase
    {
        public override int Id { get { return 65579; } }
        public override StepKind Kind { get { return StepKind.SelectMisc; } }

        private readonly string _queryOther;

        public SelectWithstringStep(string queryOther)
        {
            _queryOther = queryOther;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_queryOther);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.selectWith(_queryOther);
    }
}
