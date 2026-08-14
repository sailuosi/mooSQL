using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIsNullOR(...).</summary>
    public sealed class WhereIsNullORstringobjectstringStep : StepBase {
        public override int Id { get { return 196716; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly object _val;
        private readonly string _op;

        public WhereIsNullORstringobjectstringStep(string key, object val, string op)
        {
            _key = key;
            _val = val;
            _op = op;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                hc.Add(_op);
                return;
            }
            var emit = PassesParaRule(paraRule, _val);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
            hc.Add(_op);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereIsNullOR(_key, _val, _op);
    }
}
