using System;

namespace mooSQL.data
{
    public sealed class UnionActStep : StepBase
    {
        public override int Id { get { return 327749; } }
        public override StepKind Kind { get { return StepKind.Union; } }

        private readonly Action<SQLBuilder> _doUnion;
        public UnionActStep(Action<SQLBuilder> doUnion) { _doUnion = doUnion; }
        public override void Apply(SQLBuilder builder)
        {
            builder.Inner.union(inner =>
            {
                var facade = SQLBuilder.Attach(inner, materializing: true);
                _doUnion(facade);
            });
        }
    }
}
