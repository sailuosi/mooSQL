using System;

namespace mooSQL.data
{
    public sealed class WhereNotExistActStep : IStep
    {
        private readonly Action<SQLBuilder> _doselect;
        public WhereNotExistActStep(Action<SQLBuilder> doselect) { _doselect = doselect; }
        public void Apply(SQLBuilder builder)
            => new WhereSubqueryStep("", " NOT EXISTS ", SQLBuilder.CaptureChildSteps(_doselect)).Apply(builder);
    }
}
