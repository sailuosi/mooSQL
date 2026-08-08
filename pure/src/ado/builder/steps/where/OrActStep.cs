using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class OrActStep : IStep
    {
        private readonly Action<SQLBuilder> _doSomeWhere;

        public OrActStep(Action<SQLBuilder> doSomeWhere)
        {
            _doSomeWhere = doSomeWhere;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.or(_doSomeWhere);
    }
}
