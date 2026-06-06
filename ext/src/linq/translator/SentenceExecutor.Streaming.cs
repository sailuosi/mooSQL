#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using mooSQL.data;
using mooSQL.linq.Linq;

namespace mooSQL.linq.translator;

internal static partial class SentenceExecutor
{
    /// <summary>
    /// 逐行异步读取 SELECT（无 Includes 时使用；有 NavColumns 请走 ExecuteListAsync）。
    /// </summary>
    internal static async IAsyncEnumerable<T> StreamQueryAsync<T>(
        SentenceBag bag,
        DBInstance db,
        Expression expression,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        object?[]? parameters = null)
    {
        FinalizeBag(bag, db);
        var kit = BuildSqlBuilder(bag, db, expression, parameters);
        var sql = kit.toSelect();

        if (string.IsNullOrEmpty(sql.sql))
            yield break;

        var runner = kit.Executor ?? new DBExecutor(db);
        await foreach (var row in db.StreamQueryAsync<T>(sql, cancellationToken, runner).ConfigureAwait(false))
            yield return row;
    }
}
#endif
