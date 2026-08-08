using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLikesAnd(...).</summary>
    public sealed class WhereLikesAndstringstringArrStep : IStep
    {
        private readonly string _key;
        private readonly string[] _vals;

        public WhereLikesAndstringstringArrStep(string key, params string[] vals)
        {
            _key = key;
            _vals = vals;
        }

        public void Apply(StepBuilder builder) => builder.whereLikesAnd(_key, _vals);
    }
}
