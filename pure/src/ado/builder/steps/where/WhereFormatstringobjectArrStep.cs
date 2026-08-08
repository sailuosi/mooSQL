using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereFormat(...).</summary>
    public sealed class WhereFormatstringobjectArrStep : IStep
    {
        private readonly string _template;
        private readonly object[] _values;

        public WhereFormatstringobjectArrStep(string template, params object[] values)
        {
            _template = template;
            _values = values;
        }

        public void Apply(StepBuilder builder) => builder.whereFormat(_template, _values);
    }
}
