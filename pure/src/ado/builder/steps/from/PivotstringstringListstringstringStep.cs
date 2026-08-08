using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.pivot(...).</summary>
    public sealed class PivotstringstringListstringstringStep : IStep
    {
        private readonly string _aggregation;
        private readonly string _field;
        private readonly List<string> _values;
        private readonly string _asName;

        public PivotstringstringListstringstringStep(string aggregation, string field, List<string> values, string asName)
        {
            _aggregation = aggregation;
            _field = field;
            _values = values;
            _asName = asName;
        }

        public void Apply(StepBuilder builder) => builder.pivot(_aggregation, _field, _values, _asName);
    }
}
