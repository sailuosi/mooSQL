using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.join(...).</summary>
    public sealed class JoinstringstringstringStep : IStep
    {
        private readonly string _targetTable;
        private readonly string _onLeft;
        private readonly string _onRight;

        public JoinstringstringstringStep(string targetTable, string onLeft, string onRight)
        {
            _targetTable = targetTable;
            _onLeft = onLeft;
            _onRight = onRight;
        }

        public void Apply(StepBuilder builder) => builder.join(_targetTable, _onLeft, _onRight);
    }
}
