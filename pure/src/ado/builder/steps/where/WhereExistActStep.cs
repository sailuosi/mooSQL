using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class WhereExistActStep : IStep
    {
        private readonly Action<SQLBuilder> _doselect;

        public WhereExistActStep(Action<SQLBuilder> doselect)
        {
            _doselect = doselect;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereExist(_doselect);
    }
}
