using System;

namespace mooSQL.data
{
    public sealed class WhereInstringActStep : IStep
    {
        private readonly string _key;
        private readonly Action<SQLBuilder> _doselect;
        public WhereInstringActStep(string key, Action<SQLBuilder> doselect)
        { _key = key; _doselect = doselect; }
        public void Apply(SQLBuilder builder)
            => new WhereSubqueryStep(_key, " in ", SQLBuilder.CaptureChildSteps(_doselect)).Apply(builder);
    }
}
