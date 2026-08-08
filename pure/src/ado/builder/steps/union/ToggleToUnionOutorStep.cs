using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.toggleToUnionOutor().</summary>
    public sealed class ToggleToUnionOutorStep : IStep
    {
        public static readonly ToggleToUnionOutorStep Instance = new ToggleToUnionOutorStep();
        private ToggleToUnionOutorStep() { }
        public void Apply(SQLBuilder builder) => builder.Inner.toggleToUnionOutor();
    }
}
