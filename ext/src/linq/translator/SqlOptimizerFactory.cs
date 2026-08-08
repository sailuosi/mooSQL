using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.SqlProvider;
using System;
using System.Collections.Concurrent;

namespace mooSQL.linq.translator;

internal sealed class DefaultSqlOptimizer : BasicSqlOptimizer
{
    public DefaultSqlOptimizer(SQLProviderFlags flags) : base(flags)
    {
    }
}

internal static class SqlOptimizerFactory
{
    static readonly ConcurrentDictionary<Type, ISqlOptimizer> Cache = new();

    public static ISqlOptimizer Get(DBInstance db)
        => Cache.GetOrAdd(db.dialect.GetType(),
            _ => new DefaultSqlOptimizer(db.dialect.Option.ProviderFlags));
}
