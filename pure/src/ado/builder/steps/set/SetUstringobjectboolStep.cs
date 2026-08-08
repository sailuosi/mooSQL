using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.setU(...).</summary>
    public sealed class SetUstringobjectboolStep : IStep
    {
        private readonly string _key;
        private readonly object _val;
        private readonly bool _paramed;

        public SetUstringobjectboolStep(string key, object val, bool paramed)
        {
            _key = key;
            _val = val;
            _paramed = paramed;
        }

        public void Apply(StepBuilder builder) => builder.setU(_key, _val, _paramed);
    }
}
