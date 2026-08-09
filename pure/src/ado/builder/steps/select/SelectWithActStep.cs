using System;

namespace mooSQL.data
{
    public sealed class SelectWithActStep : IStep
    {
        private readonly Action<SQLBuilder> _queryOther;
        public SelectWithActStep(Action<SQLBuilder> queryOther) { _queryOther = queryOther; }
        public void Apply(SQLBuilder builder)
        {
            builder.Inner.selectWith(inner =>
            {
                var facade = SQLBuilder.Attach(inner, materializing: true);
                _queryOther(facade);
            });
        }
    }
}
