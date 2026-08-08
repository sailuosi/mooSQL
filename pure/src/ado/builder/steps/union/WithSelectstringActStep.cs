using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class WithSelectstringActStep : IStep
    {
        private readonly string _name;
        private readonly Action<SQLBuilder> _doselect;

        public WithSelectstringActStep(string name, Action<SQLBuilder> doselect)
        {
            _name = name;
            _doselect = doselect;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.withSelect(_name, _doselect);
    }
}
