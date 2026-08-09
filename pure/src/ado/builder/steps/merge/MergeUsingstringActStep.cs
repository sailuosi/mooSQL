using System;

namespace mooSQL.data
{
    public sealed class MergeUsingstringActStep : IStep
    {
        private readonly string _asName;
        private readonly Action<SQLBuilder> _buildSelect;
        public MergeUsingstringActStep(string asName, Action<SQLBuilder> buildSelect)
        { _asName = asName; _buildSelect = buildSelect; }
        public void Apply(SQLBuilder builder)
        {
            builder.Inner.mergeUsing(_asName, inner =>
            {
                var facade = SQLBuilder.Attach(inner, materializing: true);
                _buildSelect(facade);
            });
        }
    }
}
