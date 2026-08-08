using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereAllFieid(...).</summary>
    public sealed class WhereAllFieidEnumstringobjectstringStep : IStep
    {
        private readonly IEnumerable<string> _fields;
        private readonly object _value;
        private readonly string _op;

        public WhereAllFieidEnumstringobjectstringStep(IEnumerable<string> fields, object value, string op)
        {
            _fields = fields;
            _value = value;
            _op = op;
        }

        public void Apply(StepBuilder builder) => builder.whereAllFieid(_fields, _value, _op);
    }
}
