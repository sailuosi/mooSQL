using System;

namespace mooSQL.data
{
    public sealed class WhereORActStep : IStep
    {
        private readonly Action<SQLBuilder> _whereBuilder;
        public WhereORActStep(Action<SQLBuilder> whereBuilder) { _whereBuilder = whereBuilder; }
        public void Apply(SQLBuilder builder)
            => new WhereORSubqueryStep(SQLBuilder.CaptureChildSteps(_whereBuilder)).Apply(builder);
    }
}
