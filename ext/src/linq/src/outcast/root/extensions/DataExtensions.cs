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
		/// Linq2DB 兼容 / 表达式树 <c>Methods.LinqToDB.GetTable</c> 专用；业务代码请优先 <see cref="DBExtLinqExtension.useQueryable{T}"/>.
		/// </summary>
		public static ITable<T> GetTable<T>(this DBInstance dataContext)
			where T : notnull
			=> ExtLinqEntry.CreateTable<T>(dataContext);
	}
}
