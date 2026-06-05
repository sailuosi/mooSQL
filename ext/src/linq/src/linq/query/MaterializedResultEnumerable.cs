using System.Collections;
using System.Collections.Generic;
using System.Threading;
using mooSQL.linq.translator;

namespace mooSQL.linq.Linq;

/// <summary>
/// 内存物化结果，替代 QueryRunner 映射链的 IResultEnumerable。
/// </summary>
internal sealed class MaterializedResultEnumerable<T> : IResultEnumerable<T>
{
    readonly List<T> _items;

    public MaterializedResultEnumerable(List<T> items) => _items = items;

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#if NET5_0_OR_GREATER
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new AsyncEnumerator(_items.GetEnumerator());

    sealed class AsyncEnumerator : IAsyncEnumerator<T>
    {
        readonly IEnumerator<T> _inner;

        public AsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return default;
        }

        public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
    }
#endif
}
