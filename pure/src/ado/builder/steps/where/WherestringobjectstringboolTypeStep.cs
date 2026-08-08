using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.where(...).</summary>
    public sealed class WherestringobjectstringboolTypeStep : IStep
    {
        private readonly string _key;
        private readonly object _val;
        private readonly string _op;
        private readonly bool _paramed;
        private readonly Type _t;

        public WherestringobjectstringboolTypeStep(string key, object val, string op, bool paramed, Type t)
        {
            _key = key;
            _val = val;
            _op = op;
            _paramed = paramed;
            _t = t;
        }

        public void Apply(StepBuilder builder) => builder.where(_key, _val, _op, _paramed, _t);
    }
}
