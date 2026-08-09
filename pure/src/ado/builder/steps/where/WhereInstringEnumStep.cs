using System.Collections;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIn(...).</summary>
    public sealed class WhereInstringEnumStep : WhereListStep
    {
        public override int Id { get { return 196713; } }

        private readonly IEnumerable _values;

        public WhereInstringEnumStep(string key, IEnumerable values)
            : base(key, values)
        {
            _values = values;
        }

        public override void Apply(SQLBuilder builder)
        {
            if (_values == null) return;
            builder.Inner.whereLiveInList(Key, " IN ", () => WhereListBag.newBag(_values));
        }
    }
}
