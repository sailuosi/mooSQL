using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(...).</summary>
    public sealed class WherestringobjectTypeStep : IStep
    {
        private readonly string _key;
        private readonly object _val;
        private readonly Type _t;

        public WherestringobjectTypeStep(string key, object val, Type t)
        {
            _key = key;
            _val = val;
            _t = t;
        }

        public void Apply(StepBuilder builder) => builder.where(_key, _val, _t);
    }
}
