using System;

namespace mooSQL.data
{
    public sealed class UnionActStep : IStep
    {
        private readonly Action<SQLBuilder> _doUnion;
        public UnionActStep(Action<SQLBuilder> doUnion) { _doUnion = doUnion; }
        public void Apply(SQLBuilder builder)
        {
            builder.Inner.union(inner =>
            {
                var facade = SQLBuilder.Attach(inner, materializing: true);
                _doUnion(facade);
            });
        }
    }
}
