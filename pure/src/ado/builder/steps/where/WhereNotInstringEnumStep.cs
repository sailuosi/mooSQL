using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotIn(...).</summary>
    public sealed class WhereNotInstringEnumStep : StepBase {
        public override int Id { get { return 196733; } }
        public override StepKind Kind { get { return StepKind.Where; } }
        protected override bool HasSql
        {
            get
            {
                if (_values == null) return false;
                var e = _values as System.Collections.IEnumerable;
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
        private readonly IEnumerable _values;

        public WhereNotInstringEnumStep(string key, IEnumerable values)
        {
            _key = key;
            _values = values;
        }
        protected override void ContributeStructuralHash(ref ScriptHash hc)
        {
            hc.Add(_key);
        }


        public override void Apply(SQLBuilder builder) => builder.Inner.whereNotIn(_key, _values);
    }
}
