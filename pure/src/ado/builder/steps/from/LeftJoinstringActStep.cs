using System;

namespace mooSQL.data
{
    public sealed class LeftJoinstringActStep : IStep
    {
        private readonly string _joinSQLString;
        private readonly Action<SQLBuilder> _childFromPart;
        public LeftJoinstringActStep(string joinSQLString, Action<SQLBuilder> childFromPart)
        { _joinSQLString = joinSQLString; _childFromPart = childFromPart; }
        public void Apply(SQLBuilder builder)
            => new JoinSubqueryStep("LEFT JOIN", _joinSQLString, SQLBuilder.CaptureChildSteps(_childFromPart)).Apply(builder);
    }
}
