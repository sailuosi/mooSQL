using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.withSelect(...).</summary>
    public sealed class WithSelectstringstringStep : IStep
    {
        private readonly string _name;
        private readonly string _selectSQL;

        public WithSelectstringstringStep(string name, string selectSQL)
        {
            _name = name;
            _selectSQL = selectSQL;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.withSelect(_name, _selectSQL);
    }
}
