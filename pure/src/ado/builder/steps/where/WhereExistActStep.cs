using System;

namespace mooSQL.data
{
    public sealed class WhereExistActStep : IStep
    {
        private readonly Action<SQLBuilder> _doselect;
        public WhereExistActStep(Action<SQLBuilder> doselect) { _doselect = doselect; }
        public void Apply(SQLBuilder builder)
            => new WhereSubqueryStep("", " exists ", SQLBuilder.CaptureChildSteps(_doselect)).Apply(builder);
    }
}
