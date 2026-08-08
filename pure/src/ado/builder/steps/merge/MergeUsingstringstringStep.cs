using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.mergeUsing(...).</summary>
    public sealed class MergeUsingstringstringStep : IStep
    {
        private readonly string _asName;
        private readonly string _tabname;

        public MergeUsingstringstringStep(string asName, string tabname)
        {
            _asName = asName;
            _tabname = tabname;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.mergeUsing(_asName, _tabname);
    }
}
