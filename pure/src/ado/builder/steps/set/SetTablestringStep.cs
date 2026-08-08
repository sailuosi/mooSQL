using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setTable(...).</summary>
    public sealed class SetTablestringStep : IStep
    {
        private readonly string _tbName;

        public SetTablestringStep(string tbName)
        {
            _tbName = tbName;
        }

        public void Apply(StepBuilder builder) => builder.setTable(_tbName);
    }
}
