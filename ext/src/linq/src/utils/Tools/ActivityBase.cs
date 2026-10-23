using System;
using System.Threading.Tasks;



namespace mooSQL.linq.utils
{
	/// <summary>
	/// Provides a basic implementation of the <see cref="IActivity"/> interface.
	/// You do not have to use this class.
	/// However, it can help you to avoid incompatibility issues in the future if the <see cref="IActivity"/> interface is extended.
	/// </summary>
	
	public abstract class ActivityBase : IActivity
	{
		/// <summary>
		/// Dispose
		/// </summary>
		/// <summary>
		/// Dispose
		/// </summary>
		public abstract void Dispose();
#if NET5_0_OR_GREATER
        /// <summary>
        /// DisposeAsync
        /// </summary>
        /// <summary>
        /// DisposeAsync
        /// </summary>
        public virtual ValueTask DisposeAsync()
		{
			Dispose();
			return default;
		}
#endif

	}
}
