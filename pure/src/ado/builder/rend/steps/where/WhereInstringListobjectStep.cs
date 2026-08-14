using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIn(...).</summary>
    public sealed class WhereInstringListobjectStep : WhereListStep, ILiveBindStep
    {
        public override int Id { get { return 196714; } }

        private readonly List<object> _val;

        public WhereInstringListobjectStep(string key, List<object> val)
            : base(key, val)
        {
            _val = val;
        }

        public IDelayPara CollectLive(SQLBuilder builder)
        {
            if (_val == null) return null;
            return builder.Inner.CreateDelayWhereIn(Key, " IN ", () => WhereListBag.newBag(_val));
        }

        public override void Apply(SQLBuilder builder)
        {
            if (_val == null) return;
            builder.Inner.whereLiveInList(Key, " IN ", () => WhereListBag.newBag(_val));
        }
    }
}
