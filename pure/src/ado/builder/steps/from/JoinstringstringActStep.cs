using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.join(...).</summary>
    public sealed class JoinstringstringActStep : IStep
    {
        private readonly string _joinKey;
        private readonly string _joinSQLString;
        private readonly Action<SQLBuilder> _childFromPart;

        public JoinstringstringActStep(string joinKey, string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            _joinKey = joinKey;
            _joinSQLString = joinSQLString;
            _childFromPart = childFromPart;
        }

        public void Apply(StepBuilder builder) => builder.join(_joinKey, _joinSQLString, _childFromPart);
    }
}
