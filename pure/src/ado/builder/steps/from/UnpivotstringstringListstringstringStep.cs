using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.unpivot(...).</summary>
    public sealed class UnpivotstringstringListstringstringStep : IStep
    {
        private readonly string _valueName;
        private readonly string _fieldName;
        private readonly List<string> _fields;
        private readonly string _asName;

        public UnpivotstringstringListstringstringStep(string valueName, string fieldName, List<string> fields, string asName)
        {
            _valueName = valueName;
            _fieldName = fieldName;
            _fields = fields;
            _asName = asName;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.unpivot(_valueName, _fieldName, _fields, _asName);
    }
}
