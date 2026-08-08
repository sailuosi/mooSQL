using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.innerJoin(...).</summary>
    public sealed class InnerJoinstringStep : IStep
    {
        private readonly string _joinSQLString;

        public InnerJoinstringStep(string joinSQLString)
        {
            _joinSQLString = joinSQLString;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.innerJoin(_joinSQLString);
    }
}
