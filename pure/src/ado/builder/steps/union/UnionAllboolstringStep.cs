using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.unionAll(...).</summary>
    public sealed class UnionAllboolstringStep : IStep
    {
        private readonly bool _wrapSelect;
        private readonly string _wrapAsName;

        public UnionAllboolstringStep(bool wrapSelect, string wrapAsName)
        {
            _wrapSelect = wrapSelect;
            _wrapAsName = wrapAsName;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.unionAll(_wrapSelect, _wrapAsName);
    }
}
