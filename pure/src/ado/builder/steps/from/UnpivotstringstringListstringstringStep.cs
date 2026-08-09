using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.unpivot(...).</summary>
    public sealed class UnpivotstringstringListstringstringStep : StepBase
    {
        public override int Id { get { return 131082; } }
        public override StepKind Kind { get { return StepKind.PivotUnpivot; } }

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
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_valueName);
            hc.Add(_fieldName);
            hc.Add(_asName);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.unpivot(_valueName, _fieldName, _fields, _asName);
    }
}
