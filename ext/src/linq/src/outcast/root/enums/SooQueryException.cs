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
		public SooQueryException()
			: base("A mooSQL LINQ query exception has occurred.")
		{
		}

		public SooQueryException(string message)
			: base(message)
		{
		}

		public SooQueryException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public SooQueryException(Exception innerException)
			: base(innerException.Message, innerException)
		{
		}
	}
}
