using System;
using System.Threading.Tasks;



namespace mooSQL.linq.Tools
{
	/// <summary>
	/// Provides a basic implementation of the <see cref="IActivity"/> interface.
	/// You do not have to use this class.
	/// However, it can help you to avoid incompatibility issues in the future if the <see cref="IActivity"/> interface is extended.
	/// </summary>
	
	public abstract class ActivityBase : IActivity
	{
		public abstract void Dispose();
#if NET5_0_OR_GREATER
        public virtual ValueTask DisposeAsync()
		{
			Dispose();
			return default;
		}
#endif

	}
}
