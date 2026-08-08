using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.selectFormat(...).</summary>
    public sealed class SelectFormatstringobjectArrStep : IStep
    {
        private readonly string _selectSQLPart;
        private readonly object[] _paras;

        public SelectFormatstringobjectArrStep(string selectSQLPart, params object[] paras)
        {
            _selectSQLPart = selectSQLPart;
            _paras = paras;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.selectFormat(_selectSQLPart, _paras);
    }
}
