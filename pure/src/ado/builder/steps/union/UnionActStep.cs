using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class UnionActStep : IStep
    {
        private readonly Action<SQLBuilder> _doUnion;

        public UnionActStep(Action<SQLBuilder> doUnion)
        {
            _doUnion = doUnion;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.union(_doUnion);
    }
}
