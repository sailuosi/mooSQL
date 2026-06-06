#if NET5_0_OR_GREATER
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using mooSQL.linq.translator;

namespace mooSQL.linq.Linq;

internal sealed partial class StreamingResultEnumerable<T>
{
    public async IAsyncEnumerator<T> GetAsyncEnumerator([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in SentenceExecutor.StreamQueryAsync<T>(_bag, _db, _expression, cancellationToken, _parameters)
                           .ConfigureAwait(false))
            yield return item;
    }
}
#endif
