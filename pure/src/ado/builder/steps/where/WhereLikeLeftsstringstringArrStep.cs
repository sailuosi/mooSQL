using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereLikeLefts(...).</summary>
    public sealed class WhereLikeLeftsstringstringArrStep : IStep
    {
        private readonly string _key;
        private readonly string[] _likeCodes;

        public WhereLikeLeftsstringstringArrStep(string key, params string[] likeCodes)
        {
            _key = key;
            _likeCodes = likeCodes;
        }

        public void Apply(SQLBuilder builder) => builder.Inner.whereLikeLefts(_key, _likeCodes);
    }
}
