using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.select(...).</summary>
    public sealed class SelectstringActStep : IStep
    {
        private readonly string _asName;
        private readonly Action<SQLBuilder> _doColSelect;

        public SelectstringActStep(string asName, Action<SQLBuilder> doColSelect)
        {
            _asName = asName;
            _doColSelect = doColSelect;
        }

        public void Apply(StepBuilder builder) => builder.select(_asName, _doColSelect);
    }
}
