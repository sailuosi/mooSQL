using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.configSetNull(...).</summary>
    public sealed class ConfigSetNullUpdateSetNullOptionStep : IStep
    {
        private readonly UpdateSetNullOption _option;

        public ConfigSetNullUpdateSetNullOptionStep(UpdateSetNullOption option)
        {
            _option = option;
        }

        public void Apply(StepBuilder builder) => builder.configSetNull(_option);
    }
}
