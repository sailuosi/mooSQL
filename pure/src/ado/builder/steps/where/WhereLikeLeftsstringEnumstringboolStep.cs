using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLikeLefts(...).</summary>
    public sealed class WhereLikeLeftsstringEnumstringboolStep : IStep
    {
        private readonly string _key;
        private readonly IEnumerable<string> _vals;
        private readonly bool _isOr;

        public WhereLikeLeftsstringEnumstringboolStep(string key, IEnumerable<string> vals, bool isOr)
        {
            _key = key;
            _vals = vals;
            _isOr = isOr;
        }

        public void Apply(StepBuilder builder) => builder.whereLikeLefts(_key, _vals, _isOr);
    }
}
