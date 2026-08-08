using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.withAs(...).</summary>
    public sealed class WithAsstringActStep : IStep
    {
        private readonly string _name;
        private readonly Action<SQLBuilder> _selectBuilder;

        public WithAsstringActStep(string name, Action<SQLBuilder> selectBuilder)
        {
            _name = name;
            _selectBuilder = selectBuilder;
        }

        public void Apply(StepBuilder builder) => builder.withAs(_name, _selectBuilder);
    }
}
