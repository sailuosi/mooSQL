using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereInGuid(...)。</summary>
    public sealed class WhereInGuidstringEnumGuidNStep : WhereListStep, ILiveBindStep
    {
        public override int Id { get { return 196710; } }

        private readonly IEnumerable<Guid?> _OIDs;

        public WhereInGuidstringEnumGuidNStep(string key, IEnumerable<Guid?> OIDs)
            : base(key, OIDs)
        {
            _OIDs = OIDs;
        }

        public IDelayPara CollectLive(SQLBuilder builder)
        {
            if (_OIDs == null) return null;
            return new DelayWhereInGuid(Key, _OIDs);
        }

        public override void Apply(SQLBuilder builder)
        {
            if (_OIDs == null) return;
            builder.Inner.whereLive(new DelayWhereInGuid(Key, _OIDs));
        }
    }
}
