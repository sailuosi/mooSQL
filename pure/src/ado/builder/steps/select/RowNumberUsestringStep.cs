using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.rowNumberUse(...).</summary>
    public sealed class RowNumberUsestringStep : StepBase
    {
        public override int Id { get { return 65574; } }
        public override StepKind Kind { get { return StepKind.RowNumber; } }

        private readonly string _numFieldName;

        public RowNumberUsestringStep(string numFieldName)
        {
            _numFieldName = numFieldName;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_numFieldName);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.rowNumberUse(_numFieldName);
    }
}
