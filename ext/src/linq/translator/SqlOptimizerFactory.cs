using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.SqlProvider;

namespace mooSQL.linq.translator;

internal sealed class DefaultSqlOptimizer : BasicSqlOptimizer
{
    public DefaultSqlOptimizer(SQLProviderFlags flags) : base(flags)
    {
    }
}

internal static class SqlOptimizerFactory
{
    public static ISqlOptimizer Get(DBInstance db)
        => new DefaultSqlOptimizer(db.dialect.Option.ProviderFlags);
}
