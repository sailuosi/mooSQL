using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using mooSQL.data;
using mooSQL.linq.translator;

namespace mooSQL.linq.Linq;

/// <summary>
/// 无 LoadWith 时延迟物化；同步路径按需 ToList，异步路径走 queryAsync 逐条 yield。
/// </summary>
internal sealed class StreamingResultEnumerable<T> : IResultEnumerable<T>
{
    readonly SentenceBag _bag;
    readonly DBInstance _db;
    readonly Expression _expression;
    readonly object?[]? _parameters;

    public StreamingResultEnumerable(SentenceBag bag, DBInstance db, Expression expression, object?[]? parameters)
    {
        _bag = bag;
        _db = db;
        _expression = expression;
        _parameters = parameters;
    }

    public IEnumerator<T> GetEnumerator()
        => SentenceExecutor.ExecuteList<T>(_bag, _db, _expression, _parameters).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#if NET5_0_OR_GREATER
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        foreach (var item in await SentenceExecutor.ExecuteListAsync<T>(_bag, _db, _expression, cancellationToken, _parameters)
                     .ConfigureAwait(false))
            yield return item;
    }
#endif
}
