using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIf(...).</summary>
    public sealed class WhereIfboolNstringStep : StepBase
    {
        public override int Id { get { return 196709; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly bool? _isTrue;
        private readonly string _key;

        public WhereIfboolNstringStep(bool? isTrue, string key)
        {
            _isTrue = isTrue;
            _key = key;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                return;
            }
            var emit = true;
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.whereIf(_isTrue, _key);
    }
}
