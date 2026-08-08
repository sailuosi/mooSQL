using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class LeftJoinstringActStep : IStep
    {
        private readonly string _joinSQLString;
        private readonly Action<SQLBuilder> _childFromPart;

        public LeftJoinstringActStep(string joinSQLString, Action<SQLBuilder> childFromPart)
        {
            _joinSQLString = joinSQLString;
            _childFromPart = childFromPart;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.leftJoin(_joinSQLString, _childFromPart);
    }
}
