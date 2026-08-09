using System;

namespace mooSQL.data
{
    public sealed class WhereNotInstringActStep : IStep
    {
        private readonly string _key;
        private readonly Action<SQLBuilder> _doselect;
        public WhereNotInstringActStep(string key, Action<SQLBuilder> doselect)
        { _key = key; _doselect = doselect; }
        public void Apply(SQLBuilder builder)
            => new WhereSubqueryStep(_key, " NOT IN ", SQLBuilder.CaptureChildSteps(_doselect)).Apply(builder);
    }
}
