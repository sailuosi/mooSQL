using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.fromFormat(...).</summary>
    public sealed class FromFormatstringobjectArrStep : StepBase
    {
        public override int Id { get { return 131074; } }
        public override StepKind Kind { get { return StepKind.From; } }

        private readonly string _fromSQLPart;
        private readonly object[] _paras;

        public FromFormatstringobjectArrStep(string fromSQLPart, params object[] paras)
        {
            _fromSQLPart = fromSQLPart;
            _paras = paras;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_fromSQLPart);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.fromFormat(_fromSQLPart, _paras);
    }
}
