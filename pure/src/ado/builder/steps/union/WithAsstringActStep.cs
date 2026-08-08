using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class WithAsstringActStep : IStep
    {
        private readonly string _name;
        private readonly Action<SQLBuilder> _selectBuilder;

        public WithAsstringActStep(string name, Action<SQLBuilder> selectBuilder)
        {
            _name = name;
            _selectBuilder = selectBuilder;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.withAs(_name, _selectBuilder);
    }
}
