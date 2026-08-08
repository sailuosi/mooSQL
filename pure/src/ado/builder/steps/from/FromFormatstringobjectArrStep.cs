using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.fromFormat(...).</summary>
    public sealed class FromFormatstringobjectArrStep : IStep
    {
        private readonly string _fromSQLPart;
        private readonly object[] _paras;

        public FromFormatstringobjectArrStep(string fromSQLPart, params object[] paras)
        {
            _fromSQLPart = fromSQLPart;
            _paras = paras;
        }

        public void Apply(StepBuilder builder) => builder.fromFormat(_fromSQLPart, _paras);
    }
}
