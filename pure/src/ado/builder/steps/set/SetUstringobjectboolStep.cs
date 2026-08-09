using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setU(...).</summary>
    public sealed class SetUstringobjectboolStep : StepBase
    {
        public override int Id { get { return 262203; } }
        public override StepKind Kind { get { return StepKind.Set; } }

        private readonly string _key;
        private readonly object _val;
        private readonly bool _paramed;

        public SetUstringobjectboolStep(string key, object val, bool paramed)
        {
            _key = key;
            _val = val;
            _paramed = paramed;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
            hc.Add(_paramed);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.setU(_key, _val, _paramed);
    }
}
