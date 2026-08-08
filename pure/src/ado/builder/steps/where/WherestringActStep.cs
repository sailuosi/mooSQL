using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(...).</summary>
    public sealed class WherestringActStep : IStep
    {
        private readonly string _key;
        private readonly Action<SQLBuilder> _doselect;

        public WherestringActStep(string key, Action<SQLBuilder> doselect)
        {
            _key = key;
            _doselect = doselect;
        }

        public void Apply(StepBuilder builder) => builder.where(_key, _doselect);
    }
}
