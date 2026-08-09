using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.ifs(...).</summary>
    public sealed class IfsboolStep : StepBase
    {
        public override int Id { get { return 458753; } }
        public override StepKind Kind { get { return StepKind.Control; } }
                private readonly bool _isPass;

        public IfsboolStep(bool isPass)
        {
            _isPass = isPass;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            opened = _isPass;
            hc.Add(Id);
            hc.Add(0);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.ifs(_isPass);
    }
}
