using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIn(...).</summary>
    public sealed class WhereInstringListobjectStep : StepBase {
        public override int Id { get { return 196714; } }
        public override StepKind Kind { get { return StepKind.Where; } }
        protected override bool HasSql
        {
            get
            {
                if (_val == null) return false;
                var e = _val as System.Collections.IEnumerable;
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
        private readonly List<object> _val;

        public WhereInstringListobjectStep(string key, List<object> val)
        {
            _key = key;
            _val = val;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereIn(_key, _val);
    }
}
