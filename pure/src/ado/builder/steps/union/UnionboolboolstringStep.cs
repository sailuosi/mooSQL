using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.union(...).</summary>
    public sealed class UnionboolboolstringStep : IStep
    {
        private readonly bool _isUnionAll;
        private readonly bool _wrapSelect;
        private readonly string _wrapAsName;

        public UnionboolboolstringStep(bool isUnionAll, bool wrapSelect, string wrapAsName)
        {
            _isUnionAll = isUnionAll;
            _wrapSelect = wrapSelect;
            _wrapAsName = wrapAsName;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.union(_isUnionAll, _wrapSelect, _wrapAsName);
    }
}
