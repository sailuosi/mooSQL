using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.set(...).</summary>
    public sealed class SetstringstringintStep : IStep
    {
        private readonly string _key;
        private readonly string _value;
        private readonly int _maxLength;

        public SetstringstringintStep(string key, string value, int maxLength)
        {
            _key = key;
            _value = value;
            _maxLength = maxLength;
        }

        public void Apply(StepBuilder builder) => builder.set(_key, _value, _maxLength);
    }
}
