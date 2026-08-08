using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.leftJoin(...).</summary>
    public sealed class LeftJoinstringActStep : IStep
    {
        private readonly string _joinSQLString;
        private readonly Action<SQLBuilder> _childFromPart;

        public LeftJoinstringActStep(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            _joinSQLString = joinSQLString;
            _childFromPart = childFromPart;
        }

        public void Apply(StepBuilder builder) => builder.leftJoin(_joinSQLString, _childFromPart);
    }
}
