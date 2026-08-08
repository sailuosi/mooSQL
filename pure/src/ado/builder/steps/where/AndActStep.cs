using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class AndActStep : IStep
    {
        private readonly Action<SQLBuilder> _doSomeWhere;

        public AndActStep(Action<SQLBuilder> doSomeWhere)
        {
            _doSomeWhere = doSomeWhere;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.and(_doSomeWhere);
    }
}
