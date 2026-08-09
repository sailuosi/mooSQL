using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    public sealed class FromstringActStep : IStep
    {
        private readonly string _asName;
        private readonly Action<SQLBuilder> _childFromPart;
        public FromstringActStep(string asName, Action<SQLBuilder> childFromPart)
        { _asName = asName; _childFromPart = childFromPart; }
        public void Apply(SQLBuilder builder)
            => new FromSubqueryStep(_asName, SQLBuilder.CaptureChildSteps(_childFromPart)).Apply(builder);
    }
}
