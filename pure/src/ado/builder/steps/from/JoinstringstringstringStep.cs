using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.join(...).</summary>
    public sealed class JoinstringstringstringStep : StepBase
    {
        public override int Id { get { return 131078; } }
        public override StepKind Kind { get { return StepKind.Join; } }

        private readonly string _targetTable;
        private readonly string _onLeft;
        private readonly string _onRight;

        public JoinstringstringstringStep(string targetTable, string onLeft, string onRight)
        {
            _targetTable = targetTable;
            _onLeft = onLeft;
            _onRight = onRight;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_targetTable);
            hc.Add(_onLeft);
            hc.Add(_onRight);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.join(_targetTable, _onLeft, _onRight);
    }
}
