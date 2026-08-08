using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLikesOr(...).</summary>
    public sealed class WhereLikesOrstringstringArrStep : IStep
    {
        private readonly string _key;
        private readonly string[] _vals;

        public WhereLikesOrstringstringArrStep(string key, params string[] vals)
        {
            _key = key;
            _vals = vals;
        }

        public void Apply(StepBuilder builder) => builder.whereLikesOr(_key, _vals);
    }
}
