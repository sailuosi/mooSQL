using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereInGuid(...).</summary>
    public sealed class WhereInGuidstringEnumstringStep : StepBase {
        public override int Id { get { return 196712; } }
        public override StepKind Kind { get { return StepKind.Where; } }
        protected override bool HasSql
        {
            get
            {
                if (_OIDs == null) return false;
                var e = _OIDs as System.Collections.IEnumerable;
                if (e == null) return true;
                var it = e.GetEnumerator();
                try { return it.MoveNext(); }
                finally
                {
                    var d = it as System.IDisposable;
                    if (d != null) d.Dispose();
                }
            }
        }

        private readonly string _key;
        private readonly IEnumerable<string> _OIDs;

        public WhereInGuidstringEnumstringStep(string key, IEnumerable<string> OIDs)
        {
            _key = key;
            _OIDs = OIDs;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereInGuid(_key, _OIDs);
    }
}
