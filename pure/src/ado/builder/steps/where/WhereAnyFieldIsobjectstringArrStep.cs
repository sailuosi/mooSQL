using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereAnyFieldIs(...).</summary>
    public sealed class WhereAnyFieldIsobjectstringArrStep : IStep
    {
        private readonly object _value;
        private readonly string[] _fields;

        public WhereAnyFieldIsobjectstringArrStep(object value, params string[] fields)
        {
            _value = value;
            _fields = fields;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereAnyFieldIs(_value, _fields);
    }
}
