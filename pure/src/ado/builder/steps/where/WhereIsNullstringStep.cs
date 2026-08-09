using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIsNull(...).</summary>
    public sealed class WhereIsNullstringStep : StepBase {
        public override int Id { get { return 196717; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;

        public WhereIsNullstringStep(string key)
        {
            _key = key;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereIsNull(_key);
    }
}
