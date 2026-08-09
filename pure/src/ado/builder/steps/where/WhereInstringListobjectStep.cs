using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>对应 SQLBuilder.whereIn(...).</summary>
    public sealed class WhereInstringListobjectStep : WhereListStep
    {
        public override int Id { get { return 196714; } }

        private readonly List<object> _val;

        public WhereInstringListobjectStep(string key, List<object> val)
            : base(key, val)
        {
            _val = val;
        }

        public override void Apply(SQLBuilder builder) => builder.Inner.whereIn(Key, _val);
    }
}
