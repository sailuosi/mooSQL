using System;

namespace mooSQL.data
{
	/// <summary>
	/// Hints for Take
	/// <see cref="LinqExtensions.Take{TSource}(System.Linq.IQueryable{TSource}, int, TakeHintType)"/>
	/// <see cref="LinqExtensions.Take{TSource}(System.Linq.IQueryable{TSource}, System.Linq.Expressions.Expression{Func{int}}, TakeHintType)"/>.
	/// </summary>
	[Flags]
	public enum TakeHintType
	{
		/// <summary>
		/// SELECT TOP 10 PERCENT.
		/// </summary>
		Percent = 1,
		/// <summary>
		/// SELECT TOP 10 WITH TIES.
		/// </summary>
		WithTies = 2
	}
}
