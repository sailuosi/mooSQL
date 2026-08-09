using System;

namespace mooSQL.data
{
    public sealed class RightJoinstringActStep : IStep
    {
        private readonly string _joinSQLString;
        private readonly Action<SQLBuilder> _childFromPart;
        public RightJoinstringActStep(string joinSQLString, Action<SQLBuilder> childFromPart)
        { _joinSQLString = joinSQLString; _childFromPart = childFromPart; }
        public void Apply(SQLBuilder builder)
            => new JoinSubqueryStep("RIGHT JOIN", _joinSQLString, SQLBuilder.CaptureChildSteps(_childFromPart)).Apply(builder);
    }
}
