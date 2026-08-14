using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.pivot(...).</summary>
    public sealed class PivotstringstringListstringstringStep : StepBase
    {
        public override int Id { get { return 131081; } }
        public override StepKind Kind { get { return StepKind.PivotUnpivot; } }

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
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_aggregation);
            hc.Add(_field);
            hc.Add(_asName);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.pivot(_aggregation, _field, _values, _asName);
    }
}
