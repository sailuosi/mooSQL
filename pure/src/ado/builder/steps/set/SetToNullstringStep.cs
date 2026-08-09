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
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_fieldName);
                return;
            }
            var emit = true;
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_fieldName);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.setToNull(_fieldName);
    }
}
