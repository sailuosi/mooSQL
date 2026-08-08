using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class SelectstringActStep : IStep
    {
        private readonly string _asName;
        private readonly Action<SQLBuilder> _doColSelect;

        public SelectstringActStep(string asName, Action<SQLBuilder> doColSelect)
        {
            _asName = asName;
            _doColSelect = doColSelect;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.select(_asName, _doColSelect);
    }
}
