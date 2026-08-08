using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class WhereNotExistActStep : IStep
    {
        private readonly Action<SQLBuilder> _doselect;

        public WhereNotExistActStep(Action<SQLBuilder> doselect)
        {
            _doselect = doselect;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereNotExist(_doselect);
    }
}
