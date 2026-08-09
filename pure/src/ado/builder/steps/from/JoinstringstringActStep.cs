using System;

namespace mooSQL.data
{
    public sealed class JoinstringstringActStep : IStep
    {
        private readonly string _joinKey;
        private readonly string _joinSQLString;
        private readonly Action<SQLBuilder> _childFromPart;
        public JoinstringstringActStep(string joinKey, string joinSQLString, Action<SQLBuilder> childFromPart)
        { _joinKey = joinKey; _joinSQLString = joinSQLString; _childFromPart = childFromPart; }
        public void Apply(SQLBuilder builder)
            => new JoinSubqueryStep(_joinKey, _joinSQLString, SQLBuilder.CaptureChildSteps(_childFromPart)).Apply(builder);
    }
}
