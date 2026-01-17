#if NET451
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public interface IAsyncEnumerable<out T>
    {
        //
        // 摘要:
        //     Returns an enumerator that iterates asynchronously through the collection.
        //
        // 参数:
        //   cancellationToken:
        //     A System.Threading.CancellationToken that may be used to cancel the asynchronous
        //     iteration.
        //
        // 返回结果:
        //     An enumerator that can be used to iterate asynchronously through the collection.
#if NET5_0_OR_GREATER
        IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default(CancellationToken));
#endif

    }

    public interface IAsyncEnumerator<out T>
#if NET5_0_OR_GREATER
: IAsyncDisposable
#endif
    {
        //
        // 摘要:
        //     Gets the element in the collection at the current position of the enumerator.
        T Current { get; }

        //
        // 摘要:
        //     Advances the enumerator asynchronously to the next element of the collection.
        //
        //
        // 返回结果:
        //     A System.Threading.Tasks.ValueTask`1 that will complete with a result of true
        //     if the enumerator was successfully advanced to the next element, or false if
        //     the enumerator has passed the end of the collection.
        ValueTask<bool> MoveNextAsync();
    }

    public interface IAsyncDisposable
    {
        //
        // 摘要:
        //     Performs application-defined tasks associated with freeing, releasing, or resetting
        //     unmanaged resources asynchronously.
        //ValueTask DisposeAsync();
    }
}

#endif