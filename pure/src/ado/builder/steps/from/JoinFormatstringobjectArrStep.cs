using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.joinFormat(...).</summary>
    public sealed class JoinFormatstringobjectArrStep : StepBase
    {
        public override int Id { get { return 131076; } }
        public override StepKind Kind { get { return StepKind.Join; } }

        private readonly string _JoinSQLPart;
        private readonly object[] _paras;

        public JoinFormatstringobjectArrStep(string JoinSQLPart, params object[] paras)
        {
            _JoinSQLPart = JoinSQLPart;
            _paras = paras;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_JoinSQLPart);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.joinFormat(_JoinSQLPart, _paras);
    }
}
