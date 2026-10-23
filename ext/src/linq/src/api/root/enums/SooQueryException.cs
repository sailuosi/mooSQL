using System;
using System.Runtime.Serialization;

namespace mooSQL.linq
{
	/// <summary>
	/// Base class for mooSQL LINQ query exceptions.
	/// </summary>
	[Serializable]
	public class SooQueryException : Exception
	{
		/// <summary>
		/// SooQueryException type.
		/// </summary>
		/// <summary>
		/// SooQueryException type.
		/// </summary>
		public SooQueryException()
			: base("A mooSQL LINQ query exception has occurred.")
		{
		}
		/// <summary>
		/// SooQueryException type.
		/// </summary>

		public SooQueryException(string message)
			: base(message)
		{
		}
		/// <summary>
		/// SooQueryException type.
		/// </summary>

		public SooQueryException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
		/// <summary>
		/// SooQueryException type.
		/// </summary>

		public SooQueryException(Exception innerException)
			: base(innerException.Message, innerException)
		{
		}
	}
}
