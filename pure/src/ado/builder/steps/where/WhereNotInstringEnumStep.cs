using System.Collections;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereNotIn(...).</summary>
    public sealed class WhereNotInstringEnumStep : WhereListStep
    {
        public override int Id { get { return 196733; } }

        private readonly IEnumerable _values;

        public WhereNotInstringEnumStep(string key, IEnumerable values)
            : base(key, values)
        {
            _values = values;
        }

        public override void Apply(SQLBuilder builder)
        {
            if (_values == null) return;
            builder.Inner.whereLiveInList(Key, " NOT IN ", () => WhereListBag.newBag(_values));
        }
    }
}
