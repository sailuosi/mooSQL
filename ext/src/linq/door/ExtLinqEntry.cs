using System;
using mooSQL.linq;
using mooSQL.linq.Linq;

namespace mooSQL.data
{
    /// <summary>
    /// Ext LINQ 表 Queryable 创建入口（单一创建点）。
    /// </summary>
    internal static class ExtLinqEntry
    {
        public static IDbQuery<T> CreateDbQuery<T>(DBInstance db) where T : notnull
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            return new DbQuery<T>(db);
        }
    }
}
