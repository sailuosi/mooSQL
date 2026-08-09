using System;

namespace mooSQL.data
{
    public sealed class WhereActStep : IStep
    {
        private readonly Action<SQLBuilder> _whereBuilder;
        public WhereActStep(Action<SQLBuilder> whereBuilder) { _whereBuilder = whereBuilder; }
        public void Apply(SQLBuilder builder)
            => new WhereFragmentStep(SQLBuilder.CaptureChildSteps(_whereBuilder)).Apply(builder);
    }
}
