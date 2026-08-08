using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class WhereNotInstringActStep : IStep
    {
        private readonly string _key;
        private readonly Action<SQLBuilder> _doselect;

        public WhereNotInstringActStep(string key, Action<SQLBuilder> doselect)
        {
            _key = key;
            _doselect = doselect;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereNotIn(_key, _doselect);
    }
}
