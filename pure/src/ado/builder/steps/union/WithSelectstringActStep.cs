using System;

namespace mooSQL.data
{
    public sealed class WithSelectstringActStep : IStep
    {
        private readonly string _name;
        private readonly Action<SQLBuilder> _doselect;
        public WithSelectstringActStep(string name, Action<SQLBuilder> doselect)
        { _name = name; _doselect = doselect; }
        public void Apply(SQLBuilder builder)
            => new WithSelectSubqueryStep(_name, SQLBuilder.CaptureChildSteps(_doselect)).Apply(builder);
    }
}
