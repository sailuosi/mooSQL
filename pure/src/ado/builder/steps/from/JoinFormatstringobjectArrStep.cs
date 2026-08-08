using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.joinFormat(...).</summary>
    public sealed class JoinFormatstringobjectArrStep : IStep
    {
        private readonly string _JoinSQLPart;
        private readonly object[] _paras;

        public JoinFormatstringobjectArrStep(string JoinSQLPart, params object[] paras)
        {
            _JoinSQLPart = JoinSQLPart;
            _paras = paras;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.joinFormat(_JoinSQLPart, _paras);
    }
}
