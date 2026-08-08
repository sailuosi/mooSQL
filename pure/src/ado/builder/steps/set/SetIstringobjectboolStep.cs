using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setI(...).</summary>
    public sealed class SetIstringobjectboolStep : IStep
    {
        private readonly string _key;
        private readonly object _val;
        private readonly bool _paramed;

        public SetIstringobjectboolStep(string key, object val, bool paramed)
        {
            _key = key;
            _val = val;
            _paramed = paramed;
        }

        public void Apply(StepBuilder builder) => builder.setI(_key, _val, _paramed);
    }
}
