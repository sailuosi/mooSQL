using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLikeLeft(...).</summary>
    public sealed class WhereLikeLeftstringobjectStep : IStep
    {
        private readonly string _key;
        private readonly object _val;

        public WhereLikeLeftstringobjectStep(string key, object val)
        {
            _key = key;
            _val = val;
        }

        public void Apply(StepBuilder builder) => builder.whereLikeLeft(_key, _val);
    }
}
