using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class WhereActStep : IStep
    {
        private readonly Action<SQLBuilder> _whereBuilder;

        public WhereActStep(Action<SQLBuilder> whereBuilder)
        {
            _whereBuilder = whereBuilder;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.where(_whereBuilder);
    }
}
