using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.having(...).</summary>
    public sealed class HavingstringStep : IStep
    {
        private readonly string _havingStr;

        public HavingstringStep(string havingStr)
        {
            _havingStr = havingStr;
        }

        public void Apply(StepBuilder builder) => builder.having(_havingStr);
    }
}
