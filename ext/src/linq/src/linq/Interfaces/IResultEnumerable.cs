using System.Collections.Generic;

namespace mooSQL.linq.Linq
{
	public interface IResultEnumerable<out T> : IEnumerable<T>
#if NET5_0_OR_GREATER
		, IAsyncEnumerable<T>
#endif
	{
	}
}
