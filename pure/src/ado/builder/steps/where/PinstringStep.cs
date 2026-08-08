using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.pin(...).</summary>
    public sealed class PinstringStep : IStep
    {
        private readonly string _SQL;

        public PinstringStep(string SQL)
        {
            _SQL = SQL;
        }

        public void Apply(StepBuilder builder) => builder.pin(_SQL);
    }
}
