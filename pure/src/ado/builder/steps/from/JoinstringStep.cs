using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.join(...).</summary>
    public sealed class JoinstringStep : IStep
    {
        private readonly string _joinSQLString;

        public JoinstringStep(string joinSQLString)
        {
            _joinSQLString = joinSQLString;
        }

        public void Apply(StepBuilder builder) => builder.join(_joinSQLString);
    }
}
