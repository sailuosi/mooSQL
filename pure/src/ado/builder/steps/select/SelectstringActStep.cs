using System;

namespace mooSQL.data
{
    public sealed class SelectstringActStep : IStep
    {
        private readonly string _asName;
        private readonly Action<SQLBuilder> _doColSelect;
        public SelectstringActStep(string asName, Action<SQLBuilder> doColSelect)
        { _asName = asName; _doColSelect = doColSelect; }
        public void Apply(SQLBuilder builder)
            => new SelectSubqueryStep(_asName, SQLBuilder.CaptureChildSteps(_doColSelect)).Apply(builder);
    }
}
