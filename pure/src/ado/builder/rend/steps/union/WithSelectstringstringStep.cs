using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.withSelect(...).</summary>
    public sealed class WithSelectstringstringStep : StepBase
    {
        public override int Id { get { return 327754; } }
        public override StepKind Kind { get { return StepKind.Cte; } }

        private readonly string _name;
        private readonly string _selectSQL;

        public WithSelectstringstringStep(string name, string selectSQL)
        {
            _name = name;
            _selectSQL = selectSQL;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_name);
            hc.Add(_selectSQL);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.withSelect(_name, _selectSQL);
    }
}
