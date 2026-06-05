using System;
using mooSQL.data;
using mooSQL.linq.Linq;

namespace mooSQL.linq
{
	/// <summary>
	/// Data context extension methods.
	/// </summary>
	public static class DataExtensions
	{
		/// <summary>
		/// Returns queryable source for specified mapping class mapped to database table or view.
		/// </summary>
		public static ITable<T> GetTable<T>(this DBInstance dataContext)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			return new Table<T>(dataContext);
		}
	}
}
