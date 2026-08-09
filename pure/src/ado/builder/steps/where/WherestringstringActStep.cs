using System;

namespace mooSQL.data
{
    public sealed class WherestringstringActStep : IStep
    {
        private readonly string _key;
        private readonly string _op;
        private readonly Action<SQLBuilder> _doselect;
        public WherestringstringActStep(string key, string op, Action<SQLBuilder> doselect)
        { _key = key; _op = op; _doselect = doselect; }
        public void Apply(SQLBuilder builder)
            => new WhereSubqueryStep(_key, _op, SQLBuilder.CaptureChildSteps(_doselect)).Apply(builder);
    }
}
