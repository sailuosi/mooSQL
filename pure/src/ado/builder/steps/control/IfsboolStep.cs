using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.ifs(...).</summary>
    public sealed class IfsboolStep : IStep
    {
        private readonly bool _isPass;

        public IfsboolStep(bool isPass)
        {
            _isPass = isPass;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.ifs(_isPass);
    }
}
