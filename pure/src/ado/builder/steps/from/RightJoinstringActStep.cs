using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rightJoin(...).</summary>
    public sealed class RightJoinstringActStep : IStep
    {
        private readonly string _joinSQLString;
        private readonly Action<SQLBuilder> _childFromPart;

        public RightJoinstringActStep(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            _joinSQLString = joinSQLString;
            _childFromPart = childFromPart;
        }

        public void Apply(StepBuilder builder) => builder.rightJoin(_joinSQLString, _childFromPart);
    }
}
