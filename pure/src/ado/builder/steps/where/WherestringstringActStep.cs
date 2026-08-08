using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class WherestringstringActStep : IStep
    {
        private readonly string _key;
        private readonly string _op;
        private readonly Action<SQLBuilder> _doselect;

        public WherestringstringActStep(string key, string op, Action<SQLBuilder> doselect)
        {
            _key = key;
            _op = op;
            _doselect = doselect;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.where(_key, _op, _doselect);
    }
}
