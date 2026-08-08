using System;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class FromstringActStep : IStep
    {
        private readonly string _asName;
        private readonly Action<SQLBuilder> _childFromPart;

        public FromstringActStep(string asName, Action<SQLBuilder> childFromPart)
        {
            _asName = asName;
            _childFromPart = childFromPart;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.from(_asName, _childFromPart);
    }
}
