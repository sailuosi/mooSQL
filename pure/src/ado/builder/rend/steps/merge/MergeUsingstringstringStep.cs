using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.mergeUsing(...).</summary>
    public sealed class MergeUsingstringstringStep : StepBase
    {
        public override int Id { get { return 393231; } }
        public override StepKind Kind { get { return StepKind.Merge; } }

        private readonly string _asName;
        private readonly string _tabname;

        public MergeUsingstringstringStep(string asName, string tabname)
        {
            _asName = asName;
            _tabname = tabname;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_asName);
            hc.Add(_tabname);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.mergeUsing(_asName, _tabname);
    }
}
