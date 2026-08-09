using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setToNull(...).</summary>
    public sealed class SetToNullstringStep : StepBase
    {
        public override int Id { get { return 262202; } }
        public override StepKind Kind { get { return StepKind.Set; } }

        private readonly string _fieldName;

        public SetToNullstringStep(string fieldName)
        {
            _fieldName = fieldName;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_fieldName);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.setToNull(_fieldName);
    }
}
