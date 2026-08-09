using System;

namespace mooSQL.data
{
    public sealed class OrActStep : IStep
    {
        private readonly Action<SQLBuilder> _doSomeWhere;
        public OrActStep(Action<SQLBuilder> doSomeWhere) { _doSomeWhere = doSomeWhere; }
        public void Apply(SQLBuilder builder)
        {
            builder.Inner.or(inner =>
            {
                var facade = SQLBuilder.Attach(inner, materializing: true);
                _doSomeWhere(facade);
            });
        }
    }
}
