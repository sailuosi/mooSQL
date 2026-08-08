using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
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

        public void Apply(SQLBuilder builder) => builder.Inner.join(_joinKey, _joinSQLString, _childFromPart);
    }
}
