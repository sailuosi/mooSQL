using System;

namespace mooSQL.data
{
    public sealed class WithAsstringActStep : IStep
    {
        private readonly string _name;
        private readonly Action<SQLBuilder> _selectBuilder;
        public WithAsstringActStep(string name, Action<SQLBuilder> selectBuilder)
        { _name = name; _selectBuilder = selectBuilder; }
        public void Apply(SQLBuilder builder)
            => new WithSelectSubqueryStep(_name, SQLBuilder.CaptureChildSteps(_selectBuilder)).Apply(builder);
    }
}
