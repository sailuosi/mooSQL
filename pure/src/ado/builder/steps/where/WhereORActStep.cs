using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class WhereORActStep : IStep
    {
        private readonly Action<SQLBuilder> _whereBuilder;

        public WhereORActStep(Action<SQLBuilder> whereBuilder)
        {
            _whereBuilder = whereBuilder;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereOR(_whereBuilder);
    }
}
