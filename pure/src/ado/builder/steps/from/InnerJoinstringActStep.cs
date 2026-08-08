using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.innerJoin(...).</summary>
    public sealed class InnerJoinstringActStep : IStep
    {
        private readonly string _joinSQLString;
        private readonly Action<SQLBuilder> _childFromPart;

        public InnerJoinstringActStep(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            _joinSQLString = joinSQLString;
            _childFromPart = childFromPart;
        }

        public void Apply(StepBuilder builder) => builder.innerJoin(_joinSQLString, _childFromPart);
    }
}
