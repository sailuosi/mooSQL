using System;



namespace mooSQL.linq.utils
{
	/// <summary>
	/// Represents a user-defined operation with context to be used for Activity Service events.
	/// </summary>
	
	public interface IActivity : IDisposable
#if NET5_0_OR_GREATER
, IAsyncDisposable
#endif
    {
    }
}
