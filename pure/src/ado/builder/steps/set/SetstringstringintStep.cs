using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.set(...).</summary>
    public sealed class SetstringstringintStep : StepBase
    {
        public override int Id { get { return 262200; } }
        public override StepKind Kind { get { return StepKind.Set; } }

        private readonly string _key;
        private readonly string _value;
        private readonly int _maxLength;

        public SetstringstringintStep(string key, string value, int maxLength)
        {
            _key = key;
            _value = value;
            _maxLength = maxLength;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                hc.Add(_maxLength);
                return;
            }
            var emit = PassesParaRule(paraRule, _value);
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
            hc.Add(_maxLength);
        }
                public override void Apply(SQLBuilder builder) => builder.Inner.set(_key, _value, _maxLength);
    }
}
