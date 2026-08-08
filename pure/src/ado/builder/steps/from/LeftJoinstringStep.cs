using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.leftJoin(...).</summary>
    public sealed class LeftJoinstringStep : IStep
    {
        private readonly string _joinSQLString;

        public LeftJoinstringStep(string joinSQLString)
        {
            _joinSQLString = joinSQLString;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.leftJoin(_joinSQLString);
    }
}
