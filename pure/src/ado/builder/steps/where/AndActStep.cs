using System;

namespace mooSQL.data
{
    public sealed class AndActStep : IStep
    {
        private readonly Action<SQLBuilder> _doSomeWhere;
        public AndActStep(Action<SQLBuilder> doSomeWhere) { _doSomeWhere = doSomeWhere; }
        public void Apply(SQLBuilder builder)
        {
            builder.Inner.and(inner =>
            {
                var facade = SQLBuilder.Attach(inner, materializing: true);
                _doSomeWhere(facade);
            });
        }
    }
}
