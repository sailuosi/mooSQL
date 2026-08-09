using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.union(...).</summary>
    public sealed class UnionboolboolstringStep : StepBase
    {
        public override int Id { get { return 327752; } }
        public override StepKind Kind { get { return StepKind.Union; } }

        private readonly bool _isUnionAll;
        private readonly bool _wrapSelect;
        private readonly string _wrapAsName;

        public UnionboolboolstringStep(bool isUnionAll, bool wrapSelect, string wrapAsName)
        {
            _isUnionAll = isUnionAll;
            _wrapSelect = wrapSelect;
            _wrapAsName = wrapAsName;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_isUnionAll);
            hc.Add(_wrapSelect);
            hc.Add(_wrapAsName);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.union(_isUnionAll, _wrapSelect, _wrapAsName);
    }
}
