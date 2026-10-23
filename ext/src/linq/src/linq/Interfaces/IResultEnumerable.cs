using System.Collections.Generic;

namespace mooSQL.linq
{
	/// <summary>
	/// IResultEnumerable type.
	/// </summary>
	/// <summary>
	/// IResultEnumerable interface.
	/// </summary>
	public interface IResultEnumerable<out T> : IEnumerable<T>
#if NET5_0_OR_GREATER
		, IAsyncEnumerable<T>
#endif
	{
	}
}
