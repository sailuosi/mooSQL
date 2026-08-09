using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.configSetNull(...).</summary>
    public sealed class ConfigSetNullUpdateSetNullOptionStep : StepBase
    {
        public override int Id { get { return 262195; } }
        public override StepKind Kind { get { return StepKind.Other; } }
                private readonly UpdateSetNullOption _option;

        public ConfigSetNullUpdateSetNullOptionStep(UpdateSetNullOption option)
        {
            _option = option;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(0);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.configSetNull(_option);
    }
}
