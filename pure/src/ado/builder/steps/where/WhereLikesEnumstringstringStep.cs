using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLikes(...).</summary>
    public sealed class WhereLikesEnumstringstringStep : StepBase {
        public override int Id { get { return 196727; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly IEnumerable<string> _keys;
        private readonly string _val;

        public WhereLikesEnumstringstringStep(IEnumerable<string> keys, string val)
        {
            _keys = keys;
            _val = val;
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.whereLikes(_keys, _val);
    }
}
