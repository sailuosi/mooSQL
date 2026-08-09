using System;

namespace mooSQL.data
{
    public sealed class WherestringActStep : IStep
    {
        private readonly string _key;
        private readonly Action<SQLBuilder> _doselect;
        public WherestringActStep(string key, Action<SQLBuilder> doselect)
        { _key = key; _doselect = doselect; }
        public void Apply(SQLBuilder builder)
            => new WhereSubqueryStep(_key, "=", SQLBuilder.CaptureChildSteps(_doselect)).Apply(builder);
    }
}
