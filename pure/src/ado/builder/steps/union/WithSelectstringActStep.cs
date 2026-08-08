using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.withSelect(...).</summary>
    public sealed class WithSelectstringActStep : IStep
    {
        private readonly string _name;
        private readonly Action<SQLBuilder> _doselect;

        public WithSelectstringActStep(string name, Action<SQLBuilder> doselect)
        {
            _name = name;
            _doselect = doselect;
        }

        public void Apply(StepBuilder builder) => builder.withSelect(_name, _doselect);
    }
}
