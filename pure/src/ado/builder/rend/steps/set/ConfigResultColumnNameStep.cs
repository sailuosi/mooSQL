using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.configResultColumnName(...).</summary>
    public sealed class ConfigResultColumnNameStep : StepBase
    {
        public override int Id { get { return 262196; } }
        public override StepKind Kind { get { return StepKind.Other; } }
        private readonly ResultColumnNameMode _option;

        public ConfigResultColumnNameStep(ResultColumnNameMode option)
        {
            _option = option;
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add((int)_option);
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.configResultColumnName(_option);
    }
}
